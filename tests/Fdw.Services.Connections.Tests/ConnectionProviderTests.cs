using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections.Tests;

#pragma warning disable xUnit1051 // Sync Get overloads don't accept CancellationToken; async sibling triggers false positive

public class ConnectionProviderTests
{
    private readonly Mock<ILogger<ConnectionProvider>> _mockLogger;
    private readonly ConnectionProvider _provider;
    private readonly List<IConnectionImplementationConfiguration> _configurations;
    private readonly TestConnectionConfigurationProvider _configProvider;

    public ConnectionProviderTests()
    {
        _mockLogger = new Mock<ILogger<ConnectionProvider>>();
        _configurations = [];
        _configProvider = new TestConnectionConfigurationProvider(_configurations);

        _provider = new ConnectionProvider(_mockLogger.Object);
        _provider.Register(_configProvider);
    }

    /// <summary>
    /// Builds an implementation-provider mock of the shape the domain provider now dispatches to.
    /// The provider is what resolves a secret; the factory below it is synchronous and is not part
    /// of this test's shape at all.
    /// </summary>
    private static Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>> ProviderReturning(
        IGenericConnection connection)
    {
        var provider = new Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>();
        provider
            .Setup(x => x.Create(
                It.IsAny<IConnectionImplementationConfiguration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(connection));
        return provider;
    }

    private static Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>> ProviderFailing()
    {
        var provider = new Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>();
        provider
            .Setup(x => x.Create(
                It.IsAny<IConnectionImplementationConfiguration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Failure());
        return provider;
    }

    /// <summary>
    /// Minimal connection implementation configuration: what a domain read hands back, and what the
    /// factory is given.
    /// </summary>
    private sealed class StubConnectionConfiguration : IConnectionImplementationConfiguration
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;

        public string Implementation { get; set; } = string.Empty;

        public Guid ConnectionId { get; set; }

        public string? Description { get; set; }

        public string? Environment { get; set; }

        public bool HealthCheckEnabled { get; set; }

        public bool HealthCheckOnStartup { get; set; }

        public int? HealthCheckIntervalSeconds { get; set; }

        public bool DiscoveryEnabled { get; set; } = true;
    }

    /// <summary>The connection catalogue this provider reads, held in memory.</summary>
    private sealed class TestConnectionConfigurationProvider : IConnectionConfigurationProvider
    {
        private readonly List<IConnectionImplementationConfiguration> _configs;

        public TestConnectionConfigurationProvider(List<IConnectionImplementationConfiguration> configs)
        {
            _configs = configs ?? [];
        }

        public Task<IGenericResult<IConnectionImplementationConfiguration>> Get(string name, CancellationToken ct = default)
            => Task.FromResult(GenericResult<IConnectionImplementationConfiguration>.Success(
                _configs.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))!));

        public Task<IGenericResult<IConnectionImplementationConfiguration>> Get(Guid id, CancellationToken ct = default)
            => Task.FromResult(GenericResult<IConnectionImplementationConfiguration>.Success(
                _configs.FirstOrDefault(c => c.Id == id)!));

        public Task<IGenericResult<IConnectionImplementationConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
            => Get(id, ct);

        public Task<IGenericResult<IReadOnlyList<IConnectionImplementationConfiguration>>> Get(CancellationToken ct = default)
            => Task.FromResult(GenericResult<IReadOnlyList<IConnectionImplementationConfiguration>>.Success(_configs));

        public Task<IGenericResult<IReadOnlyList<T>>> Find<T>(Func<T, bool> predicate, CancellationToken ct = default)
            where T : IConnectionImplementationConfiguration
            => Task.FromResult(GenericResult<IReadOnlyList<T>>.Success(
                (IReadOnlyList<T>)_configs.OfType<T>().Where(predicate).ToList()));

        public Task<IGenericResult> Save<T>(
            T implementationConfiguration, string domain, string implementationName, string name, CancellationToken ct = default)
            where T : IConnectionImplementationConfiguration
            => throw new NotSupportedException("Test provider does not support Save.");

        public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
            => throw new NotSupportedException("Test provider does not support Delete.");

        public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
            => throw new NotSupportedException("Test provider does not support Delete.");

        public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
            => GenericResult.Success();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorWithValidParametersCreatesInstance()
    {
        // Arrange & Act
        var provider = new ConnectionProvider(NullLogger<ConnectionProvider>.Instance);

        // Assert
        provider.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameWithNonexistentConfigurationReturnsFailure()
    {
        // Arrange - configurations list is empty by default

        // Act
        var result = await _provider.Get("NonExistent");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByIdWithNonexistentConfigurationReturnsFailure()
    {
        // Arrange - configurations list is empty by default

        // Act
        var result = await _provider.Get(Guid.NewGuid());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameWithNoImplementationReturnsFailure()
    {
        // Arrange - add config with null Implementation
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = string.Empty
        };
        _configurations.Add(config);

        // Act - Look up by name; the config has null Implementation
        var result = await _provider.Get("TestConnection");

        // Assert - Should fail because no factory for null service type
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task RegisterWithValidParametersSucceeds()
    {
        // Arrange
        _configurations.Add(new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        });
        var mockProvider = ProviderReturning(new Mock<IGenericConnection>().Object);

        // Act
        var registerResult = _provider.Register("MsSql", () => mockProvider.Object);
        var result = await _provider.Get("TestConnection");

        // Assert
        registerResult.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RegisterWithNullNameThrowsArgumentNullException()
    {
        // The underlying dictionary uses StringComparer.OrdinalIgnoreCase
        // which throws ArgumentNullException for null keys
        var mockProvider = new Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _provider.Register(null!, () => mockProvider.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RegisterWithEmptyNameStillRegisters()
    {
        // Note: Current implementation allows empty string keys
        // This test documents current behavior
        var mockProvider = new Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>();

        // Act - Register with empty name
        Should.NotThrow(() => _provider.Register("", () => mockProvider.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RegisterWithNullFactoryStillRegisters()
    {
        // Note: Current implementation allows null values
        // This test documents current behavior
        Should.NotThrow(() => _provider.Register("MsSql", null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetWithRegisteredFactoryAndConfigurationCreatesConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IGenericConnection>();
        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        var result = await _provider.Get("TestConnection");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockConnection.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByIdWithRegisteredFactoryCreatesConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IGenericConnection>();
        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        var result = await _provider.Get(connectionId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockConnection.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetGenericWithTypeCastReturnsTypedConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        var result = await ((IDataConnectionProvider)_provider).Get<IDataConnection>("TestConnection", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockConnection.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetGenericWithIncompatibleTypeReturnsFailure()
    {
        // Arrange
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        // Return a connection that does NOT implement IDataConnection
        var mockConnection = new Mock<IGenericConnection>();

        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act - Request IDataConnection but get IGenericConnection
        var result = await ((IDataConnectionProvider)_provider).Get<IDataConnection>("TestConnection", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task IConnectionProviderExplicitImplementationDelegatesToBaseGet()
    {
        // Arrange
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IGenericConnection>();
        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act - Use explicit IConnectionProvider interface
#pragma warning disable CA1859 // deliberately uses the explicit interface
        IConnectionProvider connectionProvider = _provider;
#pragma warning restore CA1859
        var result = await connectionProvider.Get("TestConnection");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task IDataConnectionProviderExplicitImplementationDelegatesToBaseGet()
    {
        // Arrange
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act - Use explicit IDataConnectionProvider interface
        IDataConnectionProvider dataProvider = _provider;
        var result = await dataProvider.Get("TestConnection");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task IDataConnectionProviderGetByIdDelegatesToBaseGet()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        IDataConnectionProvider dataProvider = _provider;
        var result = await dataProvider.Get(connectionId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockConnection.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task IDataConnectionProviderGetGenericByNameReturnsTypedConnection()
    {
        // Arrange
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        IDataConnectionProvider dataProvider = _provider;
        var result = await dataProvider.Get<IDataConnection>("TestConnection");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockConnection.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task IDataConnectionProviderGetGenericByIdReturnsTypedConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act - IDataConnectionProvider.Get(Guid) returns IDataConnection (non-generic)
        IDataConnectionProvider dataProvider = _provider;
        var result = await dataProvider.Get(connectionId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockConnection.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByIdWithNoImplementationReturnsFailure()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = string.Empty
        };
        _configurations.Add(config);

        // Act
        var result = await _provider.Get(connectionId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetGenericByIdWithIncompatibleTypeReturnsFailure()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = "MsSql"
        };
        _configurations.Add(config);

        // Return a connection that does NOT implement IDataConnection
        var mockConnection = new Mock<IGenericConnection>();
        var mockProvider = ProviderReturning(mockConnection.Object);

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act - IDataConnectionProvider.Get(Guid) casts to IDataConnection; incompatible type → failure
        var result = await ((IDataConnectionProvider)_provider).Get(connectionId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task RegisterFactoryOverwritesPrevious()
    {
        // Arrange
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        };
        _configurations.Add(config);

        var mockConnection1 = new Mock<IGenericConnection>();
        var mockProvider1 = ProviderReturning(mockConnection1.Object);

        var mockConnection2 = new Mock<IGenericConnection>();
        var mockProvider2 = ProviderReturning(mockConnection2.Object);

        // Act
        _provider.Register("MsSql", () => mockProvider1.Object);
        _provider.Register("MsSql", () => mockProvider2.Object);

        var result = await _provider.Get("TestConnection");

        // Assert - Should use the second factory
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockConnection2.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameWhenFactoryReturnsFailureReturnsFailure()
    {
        // Arrange
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        };
        _configurations.Add(config);

        var mockProvider = ProviderFailing();

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        var result = await _provider.Get("TestConnection");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByIdWhenFactoryReturnsFailureReturnsFailure()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new StubConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            Implementation = "MsSql"
        };
        _configurations.Add(config);

        var mockProvider = ProviderFailing();

        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        var result = await _provider.Get(connectionId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameSelectsFactoryByHeaderImplementation()
    {
        // Arrange
        _configurations.Add(new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "PgConn",
            Implementation = "PostgreSql"
        });

        var msSqlConnection = new Mock<IGenericConnection>();
        var postgresConnection = new Mock<IGenericConnection>();
        _provider.Register("MsSql", () => ProviderReturning(msSqlConnection.Object).Object);
        _provider.Register("PostgreSql", () => ProviderReturning(postgresConnection.Object).Object);

        // Act
        var result = await _provider.Get("PgConn");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(postgresConnection.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetWithNamelessConfigurationReturnsFailure()
    {
        // Arrange - the cache is name-keyed, so a nameless configuration cannot be resolved
        _provider.Register("MsSql", () => ProviderReturning(new Mock<IGenericConnection>().Object).Object);

        // Act
        var result = await _provider.Get(new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = string.Empty,
            Implementation = "MsSql"
        });

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetByNameWithNoParentProviderReturnsFailure()
    {
        // Arrange - a provider whose phase-3 wiring never ran has no configuration source
        var unwired = new ConnectionProvider(NullLogger<ConnectionProvider>.Instance);

        // Act
        var result = await unwired.Get("TestConnection");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task EachMissingCreationPrerequisiteReportsItsOwnMessage()
    {
        // Arrange
        _configurations.Add(new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "NoOptionType",
            Implementation = string.Empty,
        });
        _configurations.Add(new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "NoFactory",
            Implementation = "NoSuchConnectionKind",
        });
        _configurations.Add(new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "NotAConnectionFactory",
            Implementation = "MsSql",
        });
        _provider.Register("MsSql", () => new Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>().Object);

        // Act
        var noOptionType = await _provider.Get("NoOptionType");
        var noFactory = await _provider.Get("NoFactory");
        var wrongFactoryShape = await _provider.Get("NotAConnectionFactory");

        // Assert
        noOptionType.IsSuccess.ShouldBeFalse();
        noFactory.IsSuccess.ShouldBeFalse();
        wrongFactoryShape.IsSuccess.ShouldBeFalse();
        new[] { noOptionType.CurrentMessage, noFactory.CurrentMessage, wrongFactoryShape.CurrentMessage }
            .Distinct(StringComparer.Ordinal)
            .Count()
            .ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task EveryGetBuildsItsOwnConnection()
    {
        // Arrange
        var config = new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        };
        _configurations.Add(config);
        var mockProvider = ProviderReturning(new Mock<IGenericConnection>().Object);
        _provider.Register("MsSql", () => mockProvider.Object);

        // Act - the name path and the already-resolved-configuration path. The configuration path
        // takes the IMPLEMENTATION configuration, which is what the domain provider hands back.
        var byName = await _provider.Get("TestConnection");
        var byConfiguration = await _provider.Get(
            new StubConnectionConfiguration { Id = config.Id, Name = config.Name, Implementation = config.Implementation });

        // Assert - the provider no longer caches connections, so each Get calls the factory. This
        // replaces an assertion that the two paths SHARED one cached connection.
        byName.IsSuccess.ShouldBeTrue();
        byConfiguration.IsSuccess.ShouldBeTrue();
        mockProvider.As<IAsyncServiceFactory<IGenericConnection>>().Verify(
            x => x.Create(
                It.IsAny<IGenericConfiguration>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetWithPermanentlyStaleConnectionFailsInsteadOfRecursing()
    {
        // Arrange - a connection that is stale the moment it is built. The old implementation
        // re-entered Get() on every stale result and recursed until the stack blew.
        _configurations.Add(new StubConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            Implementation = "MsSql"
        });
        var stale = new Mock<IGenericConnection>();
        stale.SetupGet(x => x.IsStale).Returns(true);
        var mockProvider = ProviderReturning(stale.Object);
        _provider.Register("MsSql", () => mockProvider.Object);

        // Act
        var result = await _provider.Get("TestConnection");

        // Assert - with no cache there is nothing to evict and rebuild: the connection is built
        // once, found stale, and fails loud.
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        mockProvider.As<IAsyncServiceFactory<IGenericConnection>>().Verify(
            x => x.Create(
                It.IsAny<IGenericConfiguration>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
