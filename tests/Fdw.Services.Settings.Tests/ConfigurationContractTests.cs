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
    public void ServerSettingImplementsIGenericConfiguration()
    {
        // Arrange
        var config = new ServerSettingConfiguration { SettingName = "TestSetting" };
        IGenericConfiguration generic = config;

        // Assert
        generic.Name.ShouldBe("TestSetting");
        generic.SectionName.ShouldBe("Settings:ServerSetting");
        generic.ServiceType.ShouldBe("ServerSetting");
        generic.ServiceOptionType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ServerSettingGenericNameSetUpdatesSettingName()
    {
        // Arrange
        var config = new ServerSettingConfiguration { SettingName = "Original" };
        IGenericConfiguration generic = config;

        // Act
        generic.Name = "Updated";

        // Assert
        config.SettingName.ShouldBe("Updated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TenantSettingImplementsIGenericConfiguration()
    {
        // Arrange
        var config = new TenantSettingConfiguration { SettingName = "TenantTest" };
        IGenericConfiguration generic = config;

        // Assert
        generic.Name.ShouldBe("TenantTest");
        generic.SectionName.ShouldBe("Settings:TenantSetting");
        generic.ServiceType.ShouldBe("TenantSetting");
        generic.ServiceOptionType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TenantSettingGenericNameSetUpdatesSettingName()
    {
        // Arrange
        var config = new TenantSettingConfiguration { SettingName = "Original" };
        IGenericConfiguration generic = config;

        // Act
        generic.Name = "Updated";

        // Assert
        config.SettingName.ShouldBe("Updated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RoleSettingImplementsIGenericConfiguration()
    {
        // Arrange
        var config = new RoleSettingConfiguration { SettingName = "RoleTest" };
        IGenericConfiguration generic = config;

        // Assert
        generic.Name.ShouldBe("RoleTest");
        generic.SectionName.ShouldBe("Settings:RoleSetting");
        generic.ServiceType.ShouldBe("RoleSetting");
        generic.ServiceOptionType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RoleSettingGenericNameSetUpdatesSettingName()
    {
        // Arrange
        var config = new RoleSettingConfiguration { SettingName = "Original" };
        IGenericConfiguration generic = config;

        // Act
        generic.Name = "Updated";

        // Assert
        config.SettingName.ShouldBe("Updated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ServerSettingDefaultsAreCorrect()
    {
        // Arrange
        var config = new ServerSettingConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.SettingName.ShouldBe(string.Empty);
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
        var config = new TenantSettingConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.TenantId.ShouldBe(Guid.Empty);
        config.SettingName.ShouldBe(string.Empty);
        config.SettingValue.ShouldBe(string.Empty);
        config.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void RoleSettingDefaultsAreCorrect()
    {
        // Arrange
        var config = new RoleSettingConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.TenantId.ShouldBe(Guid.Empty);
        config.RoleName.ShouldBe(string.Empty);
        config.SettingName.ShouldBe(string.Empty);
        config.SettingValue.ShouldBe(string.Empty);
        config.IsActive.ShouldBeTrue();
    }
}
