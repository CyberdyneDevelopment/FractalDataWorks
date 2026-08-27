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
    private readonly List<ConnectionConfiguration> _configurations;
    private readonly TestConnectionConfigurationProvider _configProvider;

    public ConnectionProviderTests()
    {
        _mockLogger = new Mock<ILogger<ConnectionProvider>>();
        _configurations = [];
        _configProvider = new TestConnectionConfigurationProvider(_configurations);

        _provider = new ConnectionProvider(new ServiceCollection().BuildServiceProvider(), _mockLogger.Object);
        // Why: the header provider is the provider's ONLY configuration source — it composes the
        // aggregate (header + typed body) and ConnectionProvider dispatches straight off it.
        _provider.Register(_configProvider);
    }

    /// <summary>
    /// Builds a factory mock of the shape the provider actually requires: an
    /// <see cref="IConnectionFactory"/> (which owns secret resolution itself) that also satisfies the
    /// <see cref="IServiceFactory{T}"/> registration signature.
    /// </summary>
    // Why: a bare IServiceFactory<IGenericConnection> mock is rejected by design
    // (FactoryNotConnectionFactory) — the async Create overload is the domain's only creation path.
    private static Mock<IConnectionFactory> FactoryReturning(IGenericConnection connection)
    {
        var factory = new Mock<IConnectionFactory>();
        factory.As<IServiceFactory<IGenericConnection>>();
        factory
            .Setup(x => x.Create(
                It.IsAny<IGenericConfiguration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(connection));
        return factory;
    }

    private static Mock<IConnectionFactory> FactoryFailing()
    {
        var factory = new Mock<IConnectionFactory>();
        factory.As<IServiceFactory<IGenericConnection>>();
        factory
            .Setup(x => x.Create(
                It.IsAny<IGenericConfiguration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Failure());
        return factory;
    }

    /// <summary>
    /// Minimal IConnectionImplementationConfiguration stub — ConnectionProvider.CreateFromHeader
    /// requires a non-null Configuration on the parent header before it dispatches to the
    /// registered factory, so the test config provider attaches one of these to every match.
    /// </summary>
    private sealed class StubConnectionConfiguration : IConnectionImplementationConfiguration
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SectionName => "Connections";
        public string ServiceType => "Connection";
        public string? ServiceOptionType { get; set; }
        public Guid ConnectionId { get; set; }
    }

    /// <summary>
    /// Simple test configuration provider for testing.
    /// </summary>
    private class TestConnectionConfigurationProvider : IServiceConfigurationProvider<ConnectionConfiguration>, IServiceConfigurationProvider
    {
        private readonly List<ConnectionConfiguration> _configs;

        public TestConnectionConfigurationProvider(List<ConnectionConfiguration> configs)
        {
            _configs = configs ?? [];
        }

        private static ConnectionConfiguration AttachStubConfig(ConnectionConfiguration cfg)
        {
            // Why: Real header providers populate Configuration via PopulateTypedBody on read.
            // The test provider stores plain configs, so attach a stub here so the factory-dispatch
            // path in ConnectionProvider.CreateFromHeader sees a non-null Configuration.
            if (cfg.Configuration is null)
                cfg.Configuration = new StubConnectionConfiguration { Id = cfg.Id, Name = cfg.Name, ServiceOptionType = cfg.ServiceOptionType };
            return cfg;
        }

        public Task<IGenericResult<ConnectionConfiguration>> Get(string name, CancellationToken cancellationToken = default)
        {
            var match = _configs.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null) AttachStubConfig(match);
            return Task.FromResult(GenericResult<ConnectionConfiguration>.Success(match!));
        }

        public Task<IGenericResult<ConnectionConfiguration>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            var match = _configs.FirstOrDefault(c => c.Id == id);
            if (match is not null) AttachStubConfig(match);
            return Task.FromResult(GenericResult<ConnectionConfiguration>.Success(match!));
        }

        public Task<IGenericResult<IReadOnlyList<ConnectionConfiguration>>> Get(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ConnectionConfiguration> list = _configs;
            return Task.FromResult(GenericResult<IReadOnlyList<ConnectionConfiguration>>.Success(list));
        }

        public Task<IGenericResult<ConnectionConfiguration>> Save(ConnectionConfiguration record, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Test provider does not support Save.");

        public Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Test provider does not support Delete.");

        public Task<IGenericResult> Delete(string name, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Test provider does not support Delete.");

        // Why: IsSystemProtected is on the interface; test provider has no ctrl configs.
        public bool IsSystemProtected(string name) => false;
    
        // Type-erased surface — delegates to the typed members.
        async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.Get(Guid id, CancellationToken ct)
        {
            var result = await Get(id, ct).ConfigureAwait(false);
            return result.IsSuccess
                ? result.ToNewResult<IGenericConfiguration>(result.Value!)
                : result.ToNewResult<IGenericConfiguration>();
        }

        async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.Get(string name, CancellationToken ct)
        {
            var result = await Get(name, ct).ConfigureAwait(false);
            return result.IsSuccess
                ? result.ToNewResult<IGenericConfiguration>(result.Value!)
                : result.ToNewResult<IGenericConfiguration>();
        }

        async Task<IGenericResult> IServiceConfigurationProvider.Save(IGenericConfiguration record, CancellationToken ct)
            => record is ConnectionConfiguration typed
                ? await Save(typed, ct).ConfigureAwait(false)
                : GenericResult.Failure(ServicesResultCodes.ByName("ServiceCastFailed"));

}

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructorWithValidParametersCreatesInstance()
    {
        // Arrange & Act
        var provider = new ConnectionProvider(new ServiceCollection().BuildServiceProvider(), NullLogger<ConnectionProvider>.Instance);

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
    public async Task GetByNameWithNoServiceOptionTypeReturnsFailure()
    {
        // Arrange - add config with null ServiceOptionType
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = null
        };
        _configurations.Add(config);

        // Act - Look up by name; the config has null ServiceOptionType
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
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        });
        var mockFactory = FactoryReturning(new Mock<IGenericConnection>().Object);

        // Act
        var registerResult = _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);
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
        var mockFactory = new Mock<IServiceFactory<IGenericConnection>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _provider.Register(null!, mockFactory.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RegisterWithEmptyNameStillRegisters()
    {
        // Note: Current implementation allows empty string keys
        // This test documents current behavior
        var mockFactory = new Mock<IServiceFactory<IGenericConnection>>();

        // Act - Register with empty name
        Should.NotThrow(() => _provider.Register("", mockFactory.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RegisterWithNullFactoryStillRegisters()
    {
        // Note: Current implementation allows null values
        // This test documents current behavior
        Should.NotThrow(() => _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetWithRegisteredFactoryAndConfigurationCreatesConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IGenericConnection>();
        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

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
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IGenericConnection>();
        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

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
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act
        // Why: Get<T> is an explicit IDataConnectionProvider interface implementation — cast required.
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
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        // Return a connection that does NOT implement IDataConnection
        var mockConnection = new Mock<IGenericConnection>();

        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act - Request IDataConnection but get IGenericConnection
        // Why: Get<T> is an explicit IDataConnectionProvider interface implementation — cast required.
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
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IGenericConnection>();
        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act - Use explicit IConnectionProvider interface
        IConnectionProvider connectionProvider = _provider;
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
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

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
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

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
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

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
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };

        _configurations.Add(config);

        var mockConnection = new Mock<IDataConnection>();
        mockConnection.As<IGenericConnection>();

        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

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
    public async Task GetByIdWithNoServiceOptionTypeReturnsFailure()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = null
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
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };
        _configurations.Add(config);

        // Return a connection that does NOT implement IDataConnection
        var mockConnection = new Mock<IGenericConnection>();
        var mockFactory = FactoryReturning(mockConnection.Object);

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act - IDataConnectionProvider.Get(Guid) casts to IDataConnection; incompatible type → failure
        // Why: No Get<T>(Guid) on IDataConnectionProvider; use non-generic Get(Guid) which casts to IDataConnection.
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
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };
        _configurations.Add(config);

        var mockConnection1 = new Mock<IGenericConnection>();
        var mockFactory1 = FactoryReturning(mockConnection1.Object);

        var mockConnection2 = new Mock<IGenericConnection>();
        var mockFactory2 = FactoryReturning(mockConnection2.Object);

        // Act
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory1.Object);
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory2.Object);

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
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };
        _configurations.Add(config);

        var mockFactory = FactoryFailing();

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

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
        var config = new ConnectionConfiguration
        {
            Id = connectionId,
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };
        _configurations.Add(config);

        var mockFactory = FactoryFailing();

        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act
        var result = await _provider.Get(connectionId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    // Why: renamed from MultipleServiceOptionTypesSearchesAllProviders, which described a mechanism
    // that does not exist — there is ONE header provider, and the header's ServiceOptionType selects
    // the FACTORY. Nothing searches a set of providers.
    public async Task GetByNameSelectsFactoryByHeaderServiceOptionType()
    {
        // Arrange
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "PgConn",
            ServiceOptionType = "PostgreSql"
        });

        var msSqlConnection = new Mock<IGenericConnection>();
        var postgresConnection = new Mock<IGenericConnection>();
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)FactoryReturning(msSqlConnection.Object).Object);
        _provider.Register("PostgreSql", (IServiceFactory<IGenericConnection>)FactoryReturning(postgresConnection.Object).Object);

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
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)FactoryReturning(new Mock<IGenericConnection>().Object).Object);

        // Act
        var result = await _provider.Get(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = string.Empty,
            ServiceOptionType = "MsSql"
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
        var unwired = new ConnectionProvider(new ServiceCollection().BuildServiceProvider(), NullLogger<ConnectionProvider>.Instance);

        // Act
        var result = await unwired.Get("TestConnection");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    // Why: the three creation prerequisites used to collapse into one "no factory registered" message,
    // so a header that simply had no ServiceOptionType reported a missing factory — the wrong problem.
    // Each gate must now name what is actually absent.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task EachMissingCreationPrerequisiteReportsItsOwnMessage()
    {
        // Arrange
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "NoOptionType",
            ServiceOptionType = null,
        });
        // Why not a real kind: a sibling test registers a PostgreSql factory into the static registry,
        // so naming PostgreSql here would find one and "NoFactory" would stop meaning what it says.
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "NoFactory",
            ServiceOptionType = "NoSuchConnectionKind",
        });
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "NotAConnectionFactory",
            ServiceOptionType = "MsSql",
        });
        // Why: a bare IServiceFactory<IGenericConnection> cannot expose the async, secret-aware Create.
        _provider.Register("MsSql", new Mock<IServiceFactory<IGenericConnection>>().Object);

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
        var config = new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        };
        _configurations.Add(config);
        var mockFactory = FactoryReturning(new Mock<IGenericConnection>().Object);
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act - the name path and the already-resolved-configuration path
        var byName = await _provider.Get("TestConnection");
        var byConfiguration = await _provider.Get(config);

        // Assert - the provider no longer caches connections, so each Get calls the factory. This
        // replaces an assertion that the two paths SHARED one cached connection.
        byName.IsSuccess.ShouldBeTrue();
        byConfiguration.IsSuccess.ShouldBeTrue();
        mockFactory.Verify(
            x => x.Create(
                It.IsAny<IGenericConfiguration>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task EvictCausesNextGetToRebuildTheConnection()
    {
        // Arrange
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        });
        var mockFactory = FactoryReturning(new Mock<IGenericConnection>().Object);
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act
        await _provider.Get("TestConnection");
        _provider.Evict("TestConnection");
        var afterEvict = await _provider.Get("TestConnection");

        // Assert
        afterEvict.IsSuccess.ShouldBeTrue();
        mockFactory.Verify(
            x => x.Create(
                It.IsAny<IGenericConfiguration>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task GetWithPermanentlyStaleConnectionFailsInsteadOfRecursing()
    {
        // Arrange - a connection that is stale the moment it is built. The old implementation
        // re-entered Get() on every stale result and recursed until the stack blew.
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        });
        var stale = new Mock<IGenericConnection>();
        stale.SetupGet(x => x.IsStale).Returns(true);
        var mockFactory = FactoryReturning(stale.Object);
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)mockFactory.Object);

        // Act
        var result = await _provider.Get("TestConnection");

        // Assert - with no cache there is nothing to evict and rebuild: the connection is built
        // once, found stale, and fails loud.
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        mockFactory.Verify(
            x => x.Create(
                It.IsAny<IGenericConfiguration>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetTypedByNameEvictsStaleConnectionLikeGetByName()
    {
        // Arrange - Get<T> used to probe the cache directly and skip the staleness check, so it
        // handed out the very connection Get(name) would have evicted.
        _configurations.Add(new ConnectionConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = "TestConnection",
            ServiceOptionType = "MsSql"
        });
        var stale = new Mock<IDataConnection>();
        stale.As<IGenericConnection>().SetupGet(x => x.IsStale).Returns(true);
        _provider.Register("MsSql", (IServiceFactory<IGenericConnection>)FactoryReturning((IGenericConnection)stale.Object).Object);

        // Act
        var result = await ((IDataConnectionProvider)_provider).Get<IDataConnection>("TestConnection", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
