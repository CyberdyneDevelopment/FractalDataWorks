using System;
using Fdw.Configuration;
using Fdw.Services.Settings.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

public sealed class ConfigurationContractTests
{






    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ServerSettingDefaultsAreCorrect()
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.Name.ShouldBe(string.Empty);
        config.SettingValue.ShouldBe(string.Empty);
        config.DataType.ShouldBe(string.Empty);
        config.Description.ShouldBeNull();
        config.MinValue.ShouldBeNull();
        config.MaxValue.ShouldBeNull();
        config.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TenantSettingDefaultsAreCorrect()
    {
        // Arrange
        var config = new TenantSettingImplementationConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.TenantId.ShouldBe(Guid.Empty);
        config.Name.ShouldBe(string.Empty);
        config.SettingValue.ShouldBe(string.Empty);
        config.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RoleSettingDefaultsAreCorrect()
    {
        // Arrange
        var config = new RoleSettingImplementationConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.TenantId.ShouldBe(Guid.Empty);
        config.RoleName.ShouldBe(string.Empty);
        config.Name.ShouldBe(string.Empty);
        config.SettingValue.ShouldBe(string.Empty);
        config.IsActive.ShouldBeTrue();
    }
}
