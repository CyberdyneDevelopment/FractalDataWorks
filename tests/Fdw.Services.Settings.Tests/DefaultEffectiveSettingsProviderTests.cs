using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Commands;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

namespace Fdw.Services.Settings.Tests;

public sealed class DefaultEffectiveSettingsProviderTests
{
    private readonly Mock<IServerSettingConfigurationProvider> _server = new(MockBehavior.Loose);
    private readonly Mock<ITenantSettingConfigurationProvider> _tenant = new(MockBehavior.Loose);
    private readonly Mock<IRoleSettingConfigurationProvider> _role = new(MockBehavior.Loose);

    private readonly List<ServerSettingImplementationConfiguration> _serverSettings = [];
    private readonly List<TenantSettingImplementationConfiguration> _tenantSettings = [];
    private readonly List<RoleSettingImplementationConfiguration> _roleSettings = [];

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    // Each domain provider is mocked on its own: the resolver reads a server setting by name and
    // the tenant and role overrides as lists, which is the whole surface it uses.
    public DefaultEffectiveSettingsProviderTests()
    {
        _server
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string name, CancellationToken _) =>
            {
                IServerSettingImplementationConfiguration? found = null;
                foreach (var setting in _serverSettings)
                {
                    if (string.Equals(setting.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        found = setting;
                        break;
                    }
                }

                return Task.FromResult<IGenericResult<IServerSettingImplementationConfiguration>>(
                    GenericResult<IServerSettingImplementationConfiguration>.Success(found!));
            });

        _tenant
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<IGenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>>(
                GenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>.Success(
                    [.. _tenantSettings.Cast<ITenantSettingImplementationConfiguration>()])));

        _role
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<IGenericResult<IReadOnlyList<IRoleSettingImplementationConfiguration>>>(
                GenericResult<IReadOnlyList<IRoleSettingImplementationConfiguration>>.Success(
                    [.. _roleSettings.Cast<IRoleSettingImplementationConfiguration>()])));
    }

    private DefaultEffectiveSettingsProvider CreateProvider() =>
        new(_server.Object, _tenant.Object, _role.Object, NullLogger<DefaultEffectiveSettingsProvider>.Instance);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueReturnsServerValueWhenNoTenantOrRole()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "500", DataType = "Int32", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows");

        // Assert
        result.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueReturnsTenantOverrideWhenWithinCeiling()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "1000", DataType = "Int32", MaxValue = "5000", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "MaxRows", SettingValue = "2000", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows", TenantA);

        // Assert
        result.ShouldBe(2000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueClampsTenantOverrideToMaxValue()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "1000", DataType = "Int32", MaxValue = "5000", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "MaxRows", SettingValue = "9999", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows", TenantA);

        // Assert
        result.ShouldBe(5000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueClampsTenantOverrideToMinValue()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "Timeout", SettingValue = "30", DataType = "Int32", MinValue = "10", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "Timeout", SettingValue = "3", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("Timeout", TenantA);

        // Assert
        result.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueReturnsRoleOverrideWithinTenantValue()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "1000", DataType = "Int32", MaxValue = "5000", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "MaxRows", SettingValue = "3000", IsActive = true });
        _roleSettings.Add(new() { TenantId = TenantA, RoleName = "Analyst", SettingName = "MaxRows", SettingValue = "2500", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows", TenantA, "Analyst");

        // Assert
        result.ShouldBe(2500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueClampsRoleOverrideToMaxValue()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "1000", DataType = "Int32", MaxValue = "5000", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "MaxRows", SettingValue = "3000", IsActive = true });
        _roleSettings.Add(new() { TenantId = TenantA, RoleName = "Admin", SettingName = "MaxRows", SettingValue = "8000", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows", TenantA, "Admin");

        // Assert
        result.ShouldBe(5000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueReturnsDefaultWhenSettingNotFound()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("NonExistent");

        // Assert
        result.ShouldBe(default);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueReturnsDefaultForNullableWhenSettingNotFound()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<string>("NonExistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueConvertsDecimalType()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "Rate", SettingValue = "99.95", DataType = "Decimal", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<decimal>("Rate");

        // Assert
        result.ShouldBe(99.95m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueConvertsBooleanType()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "EnableFeature", SettingValue = "true", DataType = "Boolean", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<bool>("EnableFeature");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueReturnsStringDirectly()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "Greeting", SettingValue = "Hello World", DataType = "String", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<string>("Greeting");

        // Assert
        result.ShouldBe("Hello World");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueReturnsDefaultForUnparseableValue()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "BadInt", SettingValue = "not-a-number", DataType = "Int32", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("BadInt");

        // Assert
        result.ShouldBe(default);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueSkipsInactiveServerSetting()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "500", DataType = "Int32", IsActive = false });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows");

        // Assert
        result.ShouldBe(default);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueSkipsInactiveTenantSetting()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "500", DataType = "Int32", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "MaxRows", SettingValue = "9999", IsActive = false });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows", TenantA);

        // Assert
        result.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueIsCaseInsensitiveOnSettingName()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "500", DataType = "Int32", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("maxrows");

        // Assert
        result.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueDoesNotClampNonNumericDataType()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "Label", SettingValue = "Default", DataType = "String", MaxValue = "100", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "Label", SettingValue = "TenantLabel", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<string>("Label", TenantA);

        // Assert
        result.ShouldBe("TenantLabel");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueSkipsInactiveRoleSetting()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "MaxRows", SettingValue = "500", DataType = "Int32", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "MaxRows", SettingValue = "2000", IsActive = true });
        _roleSettings.Add(new() { TenantId = TenantA, RoleName = "Analyst", SettingName = "MaxRows", SettingValue = "9999", IsActive = false });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<int>("MaxRows", TenantA, "Analyst");

        // Assert
        result.ShouldBe(2000);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void GetEffectiveValueClampsDecimalType()
    {
        // Arrange
        _serverSettings.Add(new() { Name = "Rate", SettingValue = "50.0", DataType = "Decimal", MinValue = "10.0", MaxValue = "100.0", IsActive = true });
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = "Rate", SettingValue = "200.5", IsActive = true });
        var provider = CreateProvider();

        // Act
        var result = provider.GetEffectiveValue<decimal>("Rate", TenantA);

        // Assert
        result.ShouldBe(100.0m);
    }
}
