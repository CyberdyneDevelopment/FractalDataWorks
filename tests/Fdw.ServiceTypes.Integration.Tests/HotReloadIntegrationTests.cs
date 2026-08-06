using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Shouldly;
using Xunit;

namespace Fdw.ServiceTypes.Integration.Tests;

/// <summary>
/// Integration tests for hot reload functionality with IOptionsMonitor
/// </summary>
public class HotReloadIntegrationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ReloadableOptionsLoader_ShouldSubscribeToOnChange()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<TestConfig>(configuration.GetSection("TestConfig"));

        var provider = services.BuildServiceProvider();

        // Act
        var monitor = provider.GetRequiredService<IOptionsMonitor<TestConfig>>();
        var initialValue = monitor.CurrentValue.Value;

        var changeCount = 0;
        var changeDisposable = monitor.OnChange((config, name) =>
        {
            changeCount++;
        });

        // Assert - Initial value
        initialValue.ShouldBe("InitialValue");

        // Act - Trigger change
        configSource.Set("TestConfig:Value", "UpdatedValue");
        configSource.TriggerReload();

        // Wait for change notification
        Thread.Sleep(100);

        // Assert - Change detected
        changeCount.ShouldBeGreaterThan(0);
        monitor.CurrentValue.Value.ShouldBe("UpdatedValue");

        changeDisposable?.Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SingletonOptionsLoader_ShouldNotReloadConfiguration()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<TestConfig>(configuration.GetSection("TestConfig"));

        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<TestConfig>>();
        var initialValue = options.Value.Value;

        // Assert - Initial value
        initialValue.ShouldBe("InitialValue");

        // Act - Trigger change (should be ignored by IOptions)
        configSource.Set("TestConfig:Value", "UpdatedValue");
        configSource.TriggerReload();

        // Wait
        Thread.Sleep(100);

        // Assert - Value unchanged (IOptions doesn't reload)
        options.Value.Value.ShouldBe("InitialValue"); // Still old value
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ScopedOptionsLoader_ShouldReloadPerScope()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<TestConfig>(configuration.GetSection("TestConfig"));

        var provider = services.BuildServiceProvider();

        // Act - Scope 1 before change
        using (var scope1 = provider.CreateScope())
        {
            var snapshot1 = scope1.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestConfig>>();
            snapshot1.Value.Value.ShouldBe("InitialValue");
        }

        // Change configuration
        configSource.Set("TestConfig:Value", "UpdatedValue");
        configSource.TriggerReload();
        Thread.Sleep(100);

        // Act - Scope 2 after change
        using (var scope2 = provider.CreateScope())
        {
            var snapshot2 = scope2.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestConfig>>();
            snapshot2.Value.Value.ShouldBe("UpdatedValue"); // New scope sees new value
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MultipleConfigurationChanges_ShouldTriggerMultipleOnChangeEvents()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<TestConfig>(configuration.GetSection("TestConfig"));

        var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<TestConfig>>();

        var changeCount = 0;
        var receivedValues = new List<string>();

        var changeDisposable = monitor.OnChange((config, name) =>
        {
            changeCount++;
            receivedValues.Add(config.Value);
        });

        // Act - Multiple changes
        configSource.Set("TestConfig:Value", "Change1");
        configSource.TriggerReload();
        Thread.Sleep(100);

        configSource.Set("TestConfig:Value", "Change2");
        configSource.TriggerReload();
        Thread.Sleep(100);

        configSource.Set("TestConfig:Value", "Change3");
        configSource.TriggerReload();
        Thread.Sleep(100);

        // Assert
        changeCount.ShouldBeGreaterThanOrEqualTo(3);
        receivedValues.ShouldContain("Change1");
        receivedValues.ShouldContain("Change2");
        receivedValues.ShouldContain("Change3");
        monitor.CurrentValue.Value.ShouldBe("Change3");

        changeDisposable?.Dispose();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void OnChangeSubscription_ShouldBeDisposable()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<TestConfig>(configuration.GetSection("TestConfig"));

        var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<TestConfig>>();

        var changeCount = 0;
        var changeDisposable = monitor.OnChange((config, name) =>
        {
            changeCount++;
        });

        // Act - Change while subscribed
        configSource.Set("TestConfig:Value", "Change1");
        configSource.TriggerReload();
        Thread.Sleep(100);

        changeCount.ShouldBe(1);

        // Dispose subscription
        changeDisposable?.Dispose();

        // Act - Change after disposal
        configSource.Set("TestConfig:Value", "Change2");
        configSource.TriggerReload();
        Thread.Sleep(100);

        // Assert - No new change notifications after disposal
        changeCount.ShouldBe(1); // Still 1, not incremented
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MultipleServices_CanUseOptionsMonitorIndependently()
    {
        // Arrange
        var configSource = new TestConfigurationSource();
        configSource.Set("Service1:Value", "Service1Initial");
        configSource.Set("Service2:Value", "Service2Initial");

        var configuration = new ConfigurationBuilder()
            .Add(configSource)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<Service1Config>(configuration.GetSection("Service1"));
        services.Configure<Service2Config>(configuration.GetSection("Service2"));

        var provider = services.BuildServiceProvider();

        var monitor1 = provider.GetRequiredService<IOptionsMonitor<Service1Config>>();
        var monitor2 = provider.GetRequiredService<IOptionsMonitor<Service2Config>>();

        var service1Changes = 0;
        var service2Changes = 0;

        var sub1 = monitor1.OnChange((config, name) => service1Changes++);
        var sub2 = monitor2.OnChange((config, name) => service2Changes++);

        // Act - Change only Service1
        configSource.Set("Service1:Value", "Service1Updated");
        configSource.TriggerReload();
        Thread.Sleep(100);

        // Assert - Only Service1 notified
        service1Changes.ShouldBeGreaterThan(0);
        monitor1.CurrentValue.Value.ShouldBe("Service1Updated");
        monitor2.CurrentValue.Value.ShouldBe("Service2Initial"); // Unchanged

        // Act - Change only Service2
        configSource.Set("Service2:Value", "Service2Updated");
        configSource.TriggerReload();
        Thread.Sleep(100);

        // Assert - Service2 updated
        monitor2.CurrentValue.Value.ShouldBe("Service2Updated");

        sub1?.Dispose();
        sub2?.Dispose();
    }
}

/// <summary>
/// Test configuration class
/// </summary>
public class TestConfig
{
    public string Value { get; set; } = "InitialValue";
}

/// <summary>
/// Service 1 configuration
/// </summary>
public class Service1Config
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Service 2 configuration
/// </summary>
public class Service2Config
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Test configuration source that supports reload triggering
/// </summary>
public class TestConfigurationSource : IConfigurationSource
{
    private TestConfigurationProvider? _provider;
    private readonly Dictionary<string, string> _initialData = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        _provider = new TestConfigurationProvider(_initialData);
        return _provider;
    }

    public void Set(string key, string value)
    {
        if (_provider != null)
        {
            _provider.Set(key, value);
        }
        else
        {
            _initialData[key] = value;
        }
    }

    public void Clear()
    {
        if (_provider != null)
        {
            _provider.Clear();
        }
        else
        {
            _initialData.Clear();
        }
    }

    public void TriggerReload()
    {
        _provider?.TriggerReload();
    }
}

/// <summary>
/// Test configuration provider that supports triggering reloads
/// </summary>
public class TestConfigurationProvider : ConfigurationProvider
{
    private readonly Dictionary<string, string?> _data;

    public TestConfigurationProvider(Dictionary<string, string>? initialData = null)
    {
        _data = initialData != null
            ? new Dictionary<string, string?>(initialData.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value))
            : new Dictionary<string, string?> { ["TestConfig:Value"] = "InitialValue" };
    }

    public override void Load()
    {
        Data = new Dictionary<string, string?>(_data);
    }

    public new void Set(string key, string value)
    {
        _data[key] = value;
        Load();
    }

    public void Clear()
    {
        _data.Clear();
        Load();
    }

    public void TriggerReload()
    {
        OnReload();
    }
}
