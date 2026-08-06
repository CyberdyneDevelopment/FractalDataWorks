using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Fdw.ServiceTypes.Integration.Tests;

/// <summary>
/// Integration tests verifying that providers rebuild their dictionaries when configuration changes
/// </summary>
public class ProviderRebuildIntegrationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ProviderWithReloadableLoader_ShouldRebuildDictionariesOnConfigChange()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        configSource.Set("Services:0:Name", "Service1");
        configSource.Set("Services:0:Value", "Initial1");
        configSource.Set("Services:1:Name", "Service2");
        configSource.Set("Services:1:Value", "Initial2");

        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<List<ServiceConfig>>(configuration.GetSection("Services"));
        services.AddSingleton<TestServiceProvider>();

        var provider = services.BuildServiceProvider();
        var testProvider = provider.GetRequiredService<TestServiceProvider>();

        // Act - Initial state
        var service1Initial = testProvider.GetService("Service1");
        var service2Initial = testProvider.GetService("Service2");

        service1Initial.ShouldNotBeNull();
        service1Initial.Value.ShouldBe("Initial1");
        service2Initial.ShouldNotBeNull();
        service2Initial.Value.ShouldBe("Initial2");

        // Act - Change configuration
        configSource.Set("Services:0:Value", "Updated1");
        configSource.Set("Services:1:Value", "Updated2");
        configSource.TriggerReload();
        Thread.Sleep(200); // Wait for OnChange to fire and rebuild

        // Assert - Provider returns services with new configuration
        var service1Updated = testProvider.GetService("Service1");
        var service2Updated = testProvider.GetService("Service2");

        service1Updated.ShouldNotBeNull();
        service1Updated.Value.ShouldBe("Updated1");
        service2Updated.ShouldNotBeNull();
        service2Updated.Value.ShouldBe("Updated2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ProviderWithReloadableLoader_ShouldHandleServicesBeingAdded()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        configSource.Set("Services:0:Name", "Service1");
        configSource.Set("Services:0:Value", "Value1");

        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<List<ServiceConfig>>(configuration.GetSection("Services"));
        services.AddSingleton<TestServiceProvider>();

        var provider = services.BuildServiceProvider();
        var testProvider = provider.GetRequiredService<TestServiceProvider>();

        // Act - Initial state (only Service1)
        var service1 = testProvider.GetService("Service1");
        var service2Missing = testProvider.GetService("Service2");

        service1.ShouldNotBeNull();
        service2Missing.ShouldBeNull();

        // Act - Add Service2 to configuration
        configSource.Set("Services:1:Name", "Service2");
        configSource.Set("Services:1:Value", "Value2");
        configSource.TriggerReload();
        Thread.Sleep(200);

        // Assert - Service2 now available
        var service2Added = testProvider.GetService("Service2");
        service2Added.ShouldNotBeNull();
        service2Added.Value.ShouldBe("Value2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ProviderWithReloadableLoader_ShouldHandleServicesBeingRemoved()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        configSource.Set("Services:0:Name", "Service1");
        configSource.Set("Services:0:Value", "Value1");
        configSource.Set("Services:1:Name", "Service2");
        configSource.Set("Services:1:Value", "Value2");

        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<List<ServiceConfig>>(configuration.GetSection("Services"));
        services.AddSingleton<TestServiceProvider>();

        var provider = services.BuildServiceProvider();
        var testProvider = provider.GetRequiredService<TestServiceProvider>();

        // Act - Initial state (both services)
        var service1 = testProvider.GetService("Service1");
        var service2 = testProvider.GetService("Service2");

        service1.ShouldNotBeNull();
        service2.ShouldNotBeNull();

        // Act - Remove Service2 from configuration
        configSource.Clear();
        configSource.Set("Services:0:Name", "Service1");
        configSource.Set("Services:0:Value", "Value1");
        configSource.TriggerReload();
        Thread.Sleep(200);

        // Assert - Service2 no longer available
        var service1Still = testProvider.GetService("Service1");
        var service2Removed = testProvider.GetService("Service2");

        service1Still.ShouldNotBeNull();
        service2Removed.ShouldBeNull(); // Service2 removed
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ProviderWithSingletonLoader_ShouldNotRebuildOnConfigChange()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        configSource.Set("Services:0:Name", "Service1");
        configSource.Set("Services:0:Value", "Initial");

        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<List<ServiceConfig>>(configuration.GetSection("Services"));
        services.AddSingleton<TestServiceProviderSingleton>();

        var provider = services.BuildServiceProvider();
        var testProvider = provider.GetRequiredService<TestServiceProviderSingleton>();

        // Act - Initial state
        var serviceInitial = testProvider.GetService("Service1");
        serviceInitial.ShouldNotBeNull();
        serviceInitial.Value.ShouldBe("Initial");

        // Act - Change configuration
        configSource.Set("Services:0:Value", "Updated");
        configSource.TriggerReload();
        Thread.Sleep(200);

        // Assert - Provider still returns old value (no rebuild with IOptions)
        var serviceAfterChange = testProvider.GetService("Service1");
        serviceAfterChange.ShouldNotBeNull();
        serviceAfterChange.Value.ShouldBe("Initial"); // Still old value
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MultipleConfigurationChanges_ShouldRebuildProviderEachTime()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        configSource.Set("Services:0:Name", "Service1");
        configSource.Set("Services:0:Value", "V1");

        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<List<ServiceConfig>>(configuration.GetSection("Services"));
        services.AddSingleton<TestServiceProvider>();

        var provider = services.BuildServiceProvider();
        var testProvider = provider.GetRequiredService<TestServiceProvider>();

        // Act & Assert - Multiple changes
        testProvider.GetService("Service1")!.Value.ShouldBe("V1");

        configSource.Set("Services:0:Value", "V2");
        configSource.TriggerReload();
        Thread.Sleep(100);
        testProvider.GetService("Service1")!.Value.ShouldBe("V2");

        configSource.Set("Services:0:Value", "V3");
        configSource.TriggerReload();
        Thread.Sleep(100);
        testProvider.GetService("Service1")!.Value.ShouldBe("V3");

        configSource.Set("Services:0:Value", "V4");
        configSource.TriggerReload();
        Thread.Sleep(100);
        testProvider.GetService("Service1")!.Value.ShouldBe("V4");
    }
}

/// <summary>
/// Service configuration
/// </summary>
public class ServiceConfig
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Simple service class
/// </summary>
public class SimpleService
{
    public string Name { get; }
    public string Value { get; }

    public SimpleService(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Test provider using IOptionsMonitor (reloadable)
/// </summary>
public class TestServiceProvider : IDisposable
{
    private readonly IOptionsMonitor<List<ServiceConfig>> _configMonitor;
    private readonly IDisposable? _changeSubscription;
    private Dictionary<string, SimpleService> _services;

    public TestServiceProvider(IOptionsMonitor<List<ServiceConfig>> configMonitor)
    {
        _configMonitor = configMonitor;
        _services = BuildServiceDictionary(configMonitor.CurrentValue);

        // Subscribe to configuration changes
        _changeSubscription = configMonitor.OnChange(configs =>
        {
            _services = BuildServiceDictionary(configs);
        });
    }

    private Dictionary<string, SimpleService> BuildServiceDictionary(List<ServiceConfig> configs)
    {
        var dict = new Dictionary<string, SimpleService>();
        if (configs != null)
        {
            foreach (var config in configs)
            {
                dict[config.Name] = new SimpleService(config.Name, config.Value);
            }
        }
        return dict;
    }

    public SimpleService? GetService(string name)
    {
        return _services.TryGetValue(name, out var service) ? service : null;
    }

    public void Dispose()
    {
        _changeSubscription?.Dispose();
    }
}

/// <summary>
/// Test provider using IOptions (singleton, no reload)
/// </summary>
public class TestServiceProviderSingleton
{
    private readonly Dictionary<string, SimpleService> _services;

    public TestServiceProviderSingleton(IOptions<List<ServiceConfig>> configOptions)
    {
        _services = BuildServiceDictionary(configOptions.Value);
        // No subscription - IOptions doesn't support change notifications
    }

    private Dictionary<string, SimpleService> BuildServiceDictionary(List<ServiceConfig> configs)
    {
        var dict = new Dictionary<string, SimpleService>();
        if (configs != null)
        {
            foreach (var config in configs)
            {
                dict[config.Name] = new SimpleService(config.Name, config.Value);
            }
        }
        return dict;
    }

    public SimpleService? GetService(string name)
    {
        return _services.TryGetValue(name, out var service) ? service : null;
    }
}
