using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Results;

namespace Fdw.Services.Tests;

/// <summary>
/// Comprehensive tests for DomainServiceProviderBase demonstrating service lifetime behaviors.
/// These tests verify that singleton providers, scoped configurations, and factory registrations
/// behave correctly across DI scopes.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class ServiceProviderLifetimeTests
{
    #region Test Infrastructure

    /// <summary>
    /// Test configuration class for service provider tests.
    /// </summary>
    public class TestServiceConfiguration : IGenericConfiguration<TestServiceConfiguration>, IImplementationConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Implementation { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test service interface.
    /// </summary>
    public interface ITestService : IGenericService
    {
        string ConfigurationValue { get; }
        Guid InstanceId { get; }
    }

    /// <summary>
    /// Test service implementation that tracks instance creation.
    /// </summary>
    public class TestService : ITestService
    {
        public string ConfigurationValue { get; }
        public Guid InstanceId { get; } = Guid.NewGuid();

        // IGenericService members
        public string Id => InstanceId.ToString();
        public string ServiceType => nameof(TestService);
        public bool IsAvailable => true;

        public TestService(string configurationValue)
        {
            ConfigurationValue = configurationValue;
        }

        public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult.Success());
        }
    }

    /// <summary>
    /// Test factory that creates TestService instances.
    /// </summary>
    /// <summary>
    /// Wraps the counting factory so the domain provider has an implementation provider to dispatch
    /// to. The factory it holds is still what the lifetime assertions read.
    /// </summary>
    public class TestServiceImplementationProvider
        : ImplementationServiceProviderBase<ITestService, TestServiceConfiguration>
    {
        public TestServiceImplementationProvider(TestServiceFactory factory) => Factory = factory;

        public TestServiceFactory Factory { get; }

        public override Task<IGenericResult<ITestService>> Create(
            TestServiceConfiguration configuration,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Factory.Create(configuration));
    }

    public class TestServiceFactory : IServiceFactory<ITestService>
    {
        public int CreateCallCount { get; private set; }
        public Guid FactoryInstanceId { get; } = Guid.NewGuid();

        public IGenericResult<ITestService> Create(IGenericConfiguration configuration)
        {
            if (configuration is TestServiceConfiguration testConfig)
            {
                CreateCallCount++;
                return GenericResult<ITestService>.Success(new TestService(testConfig.Value));
            }
            return GenericResult<ITestService>.Failure(new GenericMessage("Invalid configuration type"));
        }

        IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
        {
            var result = Create(configuration);
            if (result.IsSuccess && result.Value is T typed)
            {
                return GenericResult<T>.Success(typed);
            }
            return GenericResult<T>.Failure(result.Messages);
        }

        IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
        {
            var result = Create(configuration);
            if (result.IsSuccess)
            {
                return GenericResult<IGenericService>.Success(result.Value!);
            }
            return GenericResult<IGenericService>.Failure(result.Messages);
        }
    }

    /// <summary>
    /// Concrete test provider for testing DomainServiceProviderBase behavior.
    /// </summary>
    public class TestServiceProvider : DomainServiceProviderBase<
        ITestService,
        TestServiceConfiguration,
        IServiceFactory<ITestService>,
        IDomainConfigurationProvider<TestServiceConfiguration>>
    {
        public TestServiceProvider(ILogger<TestServiceProvider> logger)
            : base(logger)
        {
        }
    }

    /// <summary>The configurations this store holds, keyed the way the provider reads them.</summary>
    public class TestServiceConfigurationProvider : IDomainConfigurationProvider<TestServiceConfiguration>
    {
        private readonly List<TestServiceConfiguration> _configs;

        public TestServiceConfigurationProvider(List<TestServiceConfiguration> configs)
        {
            _configs = configs ?? [];
        }

        public Task<IGenericResult<TestServiceConfiguration>> Get(string name, CancellationToken ct = default)
            => Task.FromResult(GenericResult<TestServiceConfiguration>.Success(
                _configs.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))!));

        public Task<IGenericResult<TestServiceConfiguration>> Get(Guid id, CancellationToken ct = default)
            => Task.FromResult(GenericResult<TestServiceConfiguration>.Success(
                _configs.FirstOrDefault(c => c.Id == id)!));

        public Task<IGenericResult<TestServiceConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
            => Get(id, ct);

        public Task<IGenericResult<IReadOnlyList<TestServiceConfiguration>>> Get(CancellationToken ct = default)
            => Task.FromResult(GenericResult<IReadOnlyList<TestServiceConfiguration>>.Success(_configs));

        public Task<IGenericResult<IReadOnlyList<T>>> Find<T>(Func<T, bool> predicate, CancellationToken ct = default)
            where T : TestServiceConfiguration
            => Task.FromResult(GenericResult<IReadOnlyList<T>>.Success(
                (IReadOnlyList<T>)_configs.OfType<T>().Where(predicate).ToList()));

        public Task<IGenericResult> Save<T>(
            T implementationConfiguration, string domain, string implementationName, string name, CancellationToken ct = default)
            where T : TestServiceConfiguration
            => throw new NotSupportedException("Test provider does not support Save.");

        public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
            => throw new NotSupportedException("Test provider does not support Delete.");

        public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
            => throw new NotSupportedException("Test provider does not support Delete.");

        public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
            => GenericResult.Success();
    }

    /// <summary>
    /// Aggregates several stores into the one a provider reads, so a single provider can be driven
    /// with configurations that arrive from more than one place.
    /// </summary>
    public class AggregateConfigProvider : IDomainConfigurationProvider<TestServiceConfiguration>
    {
        private readonly IDomainConfigurationProvider<TestServiceConfiguration>[] _providers;

        public AggregateConfigProvider(params IDomainConfigurationProvider<TestServiceConfiguration>[] providers)
        {
            _providers = providers;
        }

        public async Task<IGenericResult<TestServiceConfiguration>> Get(string name, CancellationToken ct = default)
        {
            foreach (var provider in _providers)
            {
                var result = await provider.Get(name, ct).ConfigureAwait(false);
                if (result.IsSuccess && result.Value is not null) return result;
            }

            return GenericResult<TestServiceConfiguration>.Success(default!);
        }

        public async Task<IGenericResult<TestServiceConfiguration>> Get(Guid id, CancellationToken ct = default)
        {
            foreach (var provider in _providers)
            {
                var result = await provider.Get(id, ct).ConfigureAwait(false);
                if (result.IsSuccess && result.Value is not null) return result;
            }

            return GenericResult<TestServiceConfiguration>.Success(default!);
        }

        public Task<IGenericResult<TestServiceConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
            => Get(id, ct);

        public async Task<IGenericResult<IReadOnlyList<TestServiceConfiguration>>> Get(CancellationToken ct = default)
        {
            var all = new List<TestServiceConfiguration>();
            foreach (var provider in _providers)
            {
                var result = await provider.Get(ct).ConfigureAwait(false);
                if (result.IsSuccess && result.Value is not null) all.AddRange(result.Value);
            }

            return GenericResult<IReadOnlyList<TestServiceConfiguration>>.Success(all);
        }

        public async Task<IGenericResult<IReadOnlyList<T>>> Find<T>(Func<T, bool> predicate, CancellationToken ct = default)
            where T : TestServiceConfiguration
        {
            var all = await Get(ct).ConfigureAwait(false);
            return all.IsSuccess
                ? GenericResult<IReadOnlyList<T>>.Success((IReadOnlyList<T>)all.Value!.OfType<T>().Where(predicate).ToList())
                : all.ToNewResult<IReadOnlyList<T>>();
        }

        public Task<IGenericResult> Save<T>(
            T implementationConfiguration, string domain, string implementationName, string name, CancellationToken ct = default)
            where T : TestServiceConfiguration
            => throw new NotSupportedException("Aggregate test provider does not support Save.");

        public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
            => throw new NotSupportedException("Aggregate test provider does not support Delete.");

        public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
            => throw new NotSupportedException("Aggregate test provider does not support Delete.");

        public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
            => GenericResult.Success();
    }

    #endregion

    #region Provider Singleton Lifetime Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ProviderIsSingletonAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<List<TestServiceConfiguration>>(_ => { });

        services.AddSingleton<TestServiceProvider>();

        var rootProvider = services.BuildServiceProvider();

        // Act - Resolve from multiple scopes
        TestServiceProvider provider1, provider2, provider3;

        using (var scope1 = rootProvider.CreateScope())
        {
            provider1 = scope1.ServiceProvider.GetRequiredService<TestServiceProvider>();
        }

        using (var scope2 = rootProvider.CreateScope())
        {
            provider2 = scope2.ServiceProvider.GetRequiredService<TestServiceProvider>();
        }

        provider3 = rootProvider.GetRequiredService<TestServiceProvider>();

        // Assert - All references point to same instance
        provider1.ShouldBeSameAs(provider2);
        provider2.ShouldBeSameAs(provider3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ProviderRetainsFactoryRegistrationsAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "Value1" }
        };

        services.AddSingleton<TestServiceProvider>();

        var rootProvider = services.BuildServiceProvider();

        // Register factory AND config provider in one scope
        using (var scope1 = rootProvider.CreateScope())
        {
            var provider = scope1.ServiceProvider.GetRequiredService<TestServiceProvider>();
            var configProvider = new TestServiceConfigurationProvider(configs);
            var factory = new TestServiceFactory();
            provider.Register(configProvider);
            provider.Register(configProvider);
            provider.Register("TypeA", () => new TestServiceImplementationProvider(factory));
        }

        // Act - Get service in different scope
        IGenericResult<ITestService> result;
        using (var scope2 = rootProvider.CreateScope())
        {
            var provider = scope2.ServiceProvider.GetRequiredService<TestServiceProvider>();
            result = await provider.Get("Service1", TestContext.Current.CancellationToken);
        }

        // Assert - Factory and config provider registrations persisted
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ConfigurationValue.ShouldBe("Value1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task FactoryInstanceIsReusedAcrossServiceCreations()
    {
        // Arrange
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "V1" },
            new() { Name = "Service2", Implementation = "TypeA", Value = "V2" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        var factory = new TestServiceFactory();
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(factory));

        // Act - Get multiple services
        var result1 = await provider.Get("Service1", TestContext.Current.CancellationToken);
        var result2 = await provider.Get("Service2", TestContext.Current.CancellationToken);

        // Assert - Same factory instance used for both
        factory.CreateCallCount.ShouldBe(2);
        result1.Value!.InstanceId.ShouldNotBe(result2.Value!.InstanceId); // Different services
    }

    #endregion

    #region Configuration Provider Behavior Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConfigurationProviderProvidesConsistentValues()
    {
        // Arrange
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "Initial" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        var factory = new TestServiceFactory();
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(factory));

        // Act - Get service twice
        var result1 = await provider.Get("Service1", TestContext.Current.CancellationToken);
        var result2 = await provider.Get("Service1", TestContext.Current.CancellationToken);

        // Assert - Both calls get same configuration value
        result1.Value!.ConfigurationValue.ShouldBe("Initial");
        result2.Value!.ConfigurationValue.ShouldBe("Initial");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ProviderReturnsFailureForMissingConfiguration()
    {
        // Arrange
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "Initial" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Act
        var result = await provider.Get("NonExistentService", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConfigurationCanBeAddedDynamically()
    {
        // Arrange - start with one config
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "Value1" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Service1 exists
        var result1 = await provider.Get("Service1", TestContext.Current.CancellationToken);
        result1.IsSuccess.ShouldBeTrue();

        // Service2 doesn't exist yet
        var result2 = await provider.Get("Service2", TestContext.Current.CancellationToken);
        result2.IsSuccess.ShouldBeFalse();

        // Add Service2 dynamically
        configs.Add(new() { Name = "Service2", Implementation = "TypeA", Value = "Value2" });

        // Now Service2 exists
        var result3 = await provider.Get("Service2", TestContext.Current.CancellationToken);
        result3.IsSuccess.ShouldBeTrue();
        result3.Value!.ConfigurationValue.ShouldBe("Value2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConfigurationCanBeRemovedDynamically()
    {
        // Arrange - start with two configs
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "Value1" },
            new() { Name = "Service2", Implementation = "TypeA", Value = "Value2" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Both services exist
        var result1 = await provider.Get("Service1", TestContext.Current.CancellationToken);
        result1.IsSuccess.ShouldBeTrue();

        var result2 = await provider.Get("Service2", TestContext.Current.CancellationToken);
        result2.IsSuccess.ShouldBeTrue();

        // Remove Service2 dynamically
        var service2Config = configs.First(c => c.Name == "Service2");
        configs.Remove(service2Config);

        // Service1 still exists
        var result3 = await provider.Get("Service1", TestContext.Current.CancellationToken);
        result3.IsSuccess.ShouldBeTrue();

        // Service2 no longer exists
        var result4 = await provider.Get("Service2", TestContext.Current.CancellationToken);
        result4.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConfigurationChangesReflectedAcrossMultipleServiceTypes()
    {
        // Arrange - two service types with separate config providers
        var typeAConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "ServiceA1", Implementation = "TypeA", Value = "ValueA1" },
            new() { Name = "ServiceA2", Implementation = "TypeA", Value = "ValueA2" }
        };
        var typeBConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "ServiceB1", Implementation = "TypeB", Value = "ValueB1" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProviderA = new TestServiceConfigurationProvider(typeAConfigs);
        var configProviderB = new TestServiceConfigurationProvider(typeBConfigs);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));
        provider.Register("TypeB", () => new TestServiceImplementationProvider(new TestServiceFactory()));
        provider.Register(new AggregateConfigProvider(configProviderA, configProviderB));

        // All services exist
        (await provider.Get("ServiceA1", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await provider.Get("ServiceA2", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await provider.Get("ServiceB1", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        // Remove ServiceA2 from TypeA configs
        typeAConfigs.RemoveAll(c => c.Name == "ServiceA2");

        // ServiceA1 and ServiceB1 still exist, ServiceA2 is gone
        (await provider.Get("ServiceA1", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await provider.Get("ServiceA2", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
        (await provider.Get("ServiceB1", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        // Add new service to TypeB
        typeBConfigs.Add(new() { Name = "ServiceB2", Implementation = "TypeB", Value = "ValueB2" });

        // New service is now available
        var result = await provider.Get("ServiceB2", TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ConfigurationValue.ShouldBe("ValueB2");
    }

    #endregion

    #region Factory Registration Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task MultipleFactoriesCanBeRegisteredForDifferentTypes()
    {
        // Arrange
        var configsA = new List<TestServiceConfiguration>
        {
            new() { Name = "ServiceA", Implementation = "TypeA", Value = "ValueA" }
        };
        var configsB = new List<TestServiceConfiguration>
        {
            new() { Name = "ServiceB", Implementation = "TypeB", Value = "ValueB" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var factoryA = new TestServiceFactory();
        var factoryB = new TestServiceFactory();

        // Act
        var configProviderA = new TestServiceConfigurationProvider(configsA);
        var configProviderB = new TestServiceConfigurationProvider(configsB);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(factoryA));
        provider.Register("TypeB", () => new TestServiceImplementationProvider(factoryB));
        provider.Register(new AggregateConfigProvider(configProviderA, configProviderB));

        var resultA = await provider.Get("ServiceA", TestContext.Current.CancellationToken);
        var resultB = await provider.Get("ServiceB", TestContext.Current.CancellationToken);

        // Assert
        resultA.IsSuccess.ShouldBeTrue();
        resultB.IsSuccess.ShouldBeTrue();
        factoryA.CreateCallCount.ShouldBe(1);
        factoryB.CreateCallCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task RegisteringDuplicateFactoryOverwritesPrevious()
    {
        // Arrange
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "Value1" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        var factory1 = new TestServiceFactory();
        var factory2 = new TestServiceFactory();

        // Act
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(factory1));
        provider.Register("TypeA", () => new TestServiceImplementationProvider(factory2)); // Overwrite

        var result = await provider.Get("Service1", TestContext.Current.CancellationToken);

        // Assert
        factory1.CreateCallCount.ShouldBe(0); // Never called
        factory2.CreateCallCount.ShouldBe(1); // Used instead
    }

    #endregion

    #region Get Method Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateByNameFindsConfigurationAndUsesCorrectFactory()
    {
        // Arrange
        var msSqlConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "DatabaseConnection", Implementation = "MsSql", Value = "Server=localhost" }
        };
        var restConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "ApiConnection", Implementation = "Rest", Value = "https://api.example.com" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var msSqlFactory = new TestServiceFactory();
        var restFactory = new TestServiceFactory();

        var msSqlConfigProvider = new TestServiceConfigurationProvider(msSqlConfigs);
        var restConfigProvider = new TestServiceConfigurationProvider(restConfigs);
        provider.Register(msSqlConfigProvider);
        provider.Register("MsSql", () => new TestServiceImplementationProvider(msSqlFactory));
        provider.Register(restConfigProvider);
        provider.Register("Rest", () => new TestServiceImplementationProvider(restFactory));
        provider.Register(new AggregateConfigProvider(msSqlConfigProvider, restConfigProvider));

        // Act
        var dbResult = await provider.Get("DatabaseConnection", TestContext.Current.CancellationToken);
        var apiResult = await provider.Get("ApiConnection", TestContext.Current.CancellationToken);

        // Assert
        dbResult.IsSuccess.ShouldBeTrue();
        dbResult.Value!.ConfigurationValue.ShouldBe("Server=localhost");
        msSqlFactory.CreateCallCount.ShouldBe(1);

        apiResult.IsSuccess.ShouldBeTrue();
        apiResult.Value!.ConfigurationValue.ShouldBe("https://api.example.com");
        restFactory.CreateCallCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateByIdFindsConfigurationCorrectly()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var configs = new List<TestServiceConfiguration>
        {
            new() { Id = serviceId, Name = "Service1", Implementation = "TypeA", Value = "TestValue" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Act
        var result = await provider.Get(serviceId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ConfigurationValue.ShouldBe("TestValue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateByConfigurationUsesProvidedConfiguration()
    {
        // Arrange
        var customConfig = new TestServiceConfiguration
        {
            Name = "CustomService",
            Implementation = "TypeA",
            Value = "CustomValue"
        };

        var configs = new List<TestServiceConfiguration> { customConfig };
        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Act - Look up by name since Get(configuration) was removed
        var result = await provider.Get("CustomService", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ConfigurationValue.ShouldBe("CustomValue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateWithNullNameReturnsFailure()
    {
        // Arrange
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = "TypeA", Value = "Value1" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);
        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Act
        var result = await provider.Get((string)null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task CreateWithMissingImplementationReturnsFailure()
    {
        var configs = new List<TestServiceConfiguration>
        {
            new() { Name = "Service1", Implementation = string.Empty, Value = "Value" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);
        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Act
        var result = await provider.Get("Service1", TestContext.Current.CancellationToken);

        // Assert - Fails because Implementation is null and parent can't dispatch
        result.IsSuccess.ShouldBeFalse();
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConcurrentServiceCreationIsThreadSafe()
    {
        // Arrange
        var configs = Enumerable.Range(1, 100)
            .Select(i => new TestServiceConfiguration
            {
                Name = $"Service{i}",
                Implementation = "TypeA",
                Value = $"Value{i}"
            })
            .ToList();

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var configProvider = new TestServiceConfigurationProvider(configs);
        provider.Register(configProvider);
        provider.Register(configProvider);
        provider.Register("TypeA", () => new TestServiceImplementationProvider(new TestServiceFactory()));

        // Act - Get services concurrently using Parallel.For
        var results = new IGenericResult<ITestService>[configs.Count];
        Parallel.For(0, configs.Count, i =>
        {
#pragma warning disable VSTHRD002
            results[i] = provider.Get(configs[i].Name).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        });

        // Assert - All should succeed
        results.ShouldAllBe(r => r.IsSuccess);
        results.Select(r => r.Value!.ConfigurationValue).Distinct().Count().ShouldBe(100);
    }

    #endregion

    #region Full Integration Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task FullDIIntegrationWithConfigureAndRegister()
    {
        // Arrange
        var typeAConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "PrimaryService", Implementation = "TypeA", Value = "PrimaryValue" }
        };
        var typeBConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "SecondaryService", Implementation = "TypeB", Value = "SecondaryValue" }
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TestServiceFactory>();
        services.AddSingleton<TestServiceProvider>();

        var rootProvider = services.BuildServiceProvider();

        // Initialize provider with factories and config providers (simulating what generated code does)
        var testProvider = rootProvider.GetRequiredService<TestServiceProvider>();
        var factory = rootProvider.GetRequiredService<TestServiceFactory>();
        var configProviderA = new TestServiceConfigurationProvider(typeAConfigs);
        var configProviderB = new TestServiceConfigurationProvider(typeBConfigs);
        testProvider.Register("TypeA", () => new TestServiceImplementationProvider(factory));
        testProvider.Register("TypeB", () => new TestServiceImplementationProvider(factory));
        testProvider.Register(new AggregateConfigProvider(configProviderA, configProviderB));

        // Act
        var result1 = await testProvider.Get("PrimaryService", TestContext.Current.CancellationToken);
        var result2 = await testProvider.Get("SecondaryService", TestContext.Current.CancellationToken);

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result1.Value!.ConfigurationValue.ShouldBe("PrimaryValue");

        result2.IsSuccess.ShouldBeTrue();
        result2.Value!.ConfigurationValue.ShouldBe("SecondaryValue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ProviderCorrectlyRoutesToFactoryBasedOnImplementation()
    {
        // Arrange - simulate polymorphic configuration (MsSql vs Rest)
        var msSqlConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "OrdersDb", Implementation = "MsSql", Value = "Server=orders-db" },
            new() { Name = "InventoryDb", Implementation = "MsSql", Value = "Server=inventory-db" }
        };
        var restConfigs = new List<TestServiceConfiguration>
        {
            new() { Name = "PaymentApi", Implementation = "Rest", Value = "https://payment.api" },
            new() { Name = "ShippingApi", Implementation = "Rest", Value = "https://shipping.api" }
        };

        var provider = new TestServiceProvider(NullLogger<TestServiceProvider>.Instance);

        var msSqlFactory = new TestServiceFactory();
        var restFactory = new TestServiceFactory();

        var msSqlConfigProvider = new TestServiceConfigurationProvider(msSqlConfigs);
        var restConfigProvider = new TestServiceConfigurationProvider(restConfigs);
        provider.Register(msSqlConfigProvider);
        provider.Register("MsSql", () => new TestServiceImplementationProvider(msSqlFactory));
        provider.Register(restConfigProvider);
        provider.Register("Rest", () => new TestServiceImplementationProvider(restFactory));
        provider.Register(new AggregateConfigProvider(msSqlConfigProvider, restConfigProvider));

        // Act
        var ordersResult = await provider.Get("OrdersDb", TestContext.Current.CancellationToken);
        var inventoryResult = await provider.Get("InventoryDb", TestContext.Current.CancellationToken);
        var paymentResult = await provider.Get("PaymentApi", TestContext.Current.CancellationToken);
        var shippingResult = await provider.Get("ShippingApi", TestContext.Current.CancellationToken);

        // Assert - correct factories were used
        msSqlFactory.CreateCallCount.ShouldBe(2); // OrdersDb + InventoryDb
        restFactory.CreateCallCount.ShouldBe(2); // PaymentApi + ShippingApi

        ordersResult.Value!.ConfigurationValue.ShouldBe("Server=orders-db");
        paymentResult.Value!.ConfigurationValue.ShouldBe("https://payment.api");
    }

    #endregion
}
