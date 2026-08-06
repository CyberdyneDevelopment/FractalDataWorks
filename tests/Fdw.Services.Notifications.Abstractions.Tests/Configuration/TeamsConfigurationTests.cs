using Fdw.Services.Notifications.Abstractions.Configuration;

namespace Fdw.Services.Notifications.Abstractions.Tests.Configuration;

/// <summary>
/// Tests for TeamsConfiguration class.
/// </summary>
public class TeamsConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorSetsExpectedDefaults()
    {
        // Arrange & Act
        var config = new TeamsConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.Name.ShouldBe("Teams");
        config.SectionName.ShouldBe("Teams");
        config.ServiceType.ShouldBe("Notification");
        config.ServiceOptionType.ShouldBe("Teams");
        config.NotificationType.ShouldBe("Teams");
        config.IsEnabled.ShouldBeTrue();
        config.DefaultWebhookUrl.ShouldBeNull();
        config.TimeoutSeconds.ShouldBe(30);
        config.UseAdaptiveCards.ShouldBeTrue();
        config.SecretManagerName.ShouldBeNull();
        config.SecretKeyName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void PropertiesCanBeSetAndRetrieved()
    {
        // Arrange
        var config = new TeamsConfiguration();
        var id = Guid.NewGuid();

        // Act
        config.Id = id;
        config.Name = "CustomTeams";
        config.NotificationType = "CustomType";
        config.IsEnabled = false;
        config.DefaultWebhookUrl = "https://teams.webhook.url";
        config.TimeoutSeconds = 60;
        config.UseAdaptiveCards = false;
        config.SecretManagerName = "MySecretManager";
        config.SecretKeyName = "MySecretKey";

        // Assert
        config.Id.ShouldBe(id);
        config.Name.ShouldBe("CustomTeams");
        config.NotificationType.ShouldBe("CustomType");
        config.ServiceOptionType.ShouldBe("CustomType");
        config.IsEnabled.ShouldBeFalse();
        config.DefaultWebhookUrl.ShouldBe("https://teams.webhook.url");
        config.TimeoutSeconds.ShouldBe(60);
        config.UseAdaptiveCards.ShouldBeFalse();
        config.SecretManagerName.ShouldBe("MySecretManager");
        config.SecretKeyName.ShouldBe("MySecretKey");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ServiceOptionTypeMatchesNotificationType()
    {
        // Arrange
        var config = new TeamsConfiguration
        {
            NotificationType = "CustomTeamsType"
        };

        // Act & Assert
        config.ServiceOptionType.ShouldBe(config.NotificationType);
        config.ServiceOptionType.ShouldBe("CustomTeamsType");
    }
}
