using Fdw.Services.Notifications.Abstractions.Configuration;

namespace Fdw.Services.Notifications.Abstractions.Tests.Configuration;

/// <summary>
/// Tests for WebhookConfiguration class.
/// </summary>
public class WebhookConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorSetsExpectedDefaults()
    {
        // Arrange & Act
        var config = new WebhookConfiguration();

        // Assert
        config.Id.ShouldNotBe(Guid.Empty);
        config.Name.ShouldBe("Webhook");
        config.SectionName.ShouldBe("Webhook");
        config.ServiceType.ShouldBe("Notification");
        config.ServiceOptionType.ShouldBe("Webhook");
        config.NotificationType.ShouldBe("Webhook");
        config.IsEnabled.ShouldBeTrue();
        config.DefaultWebhookUrl.ShouldBeNull();
        config.TimeoutSeconds.ShouldBe(30);
        config.HttpMethod.ShouldBe("POST");
        config.Headers.ShouldBeNull();
        config.SecretManagerName.ShouldBeNull();
        config.SecretKeyName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void PropertiesCanBeSetAndRetrieved()
    {
        // Arrange
        var config = new WebhookConfiguration();
        var id = Guid.NewGuid();
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" };

        // Act
        config.Id = id;
        config.Name = "CustomWebhook";
        config.NotificationType = "CustomType";
        config.IsEnabled = false;
        config.DefaultWebhookUrl = "https://webhook.url";
        config.TimeoutSeconds = 60;
        config.HttpMethod = "PUT";
        config.Headers = headers;
        config.SecretManagerName = "MySecretManager";
        config.SecretKeyName = "MySecretKey";

        // Assert
        config.Id.ShouldBe(id);
        config.Name.ShouldBe("CustomWebhook");
        config.NotificationType.ShouldBe("CustomType");
        config.ServiceOptionType.ShouldBe("CustomType");
        config.IsEnabled.ShouldBeFalse();
        config.DefaultWebhookUrl.ShouldBe("https://webhook.url");
        config.TimeoutSeconds.ShouldBe(60);
        config.HttpMethod.ShouldBe("PUT");
        config.Headers.ShouldBe(headers);
        config.SecretManagerName.ShouldBe("MySecretManager");
        config.SecretKeyName.ShouldBe("MySecretKey");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ServiceOptionTypeMatchesNotificationType()
    {
        // Arrange
        var config = new WebhookConfiguration
        {
            NotificationType = "CustomWebhookType"
        };

        // Act & Assert
        config.ServiceOptionType.ShouldBe(config.NotificationType);
        config.ServiceOptionType.ShouldBe("CustomWebhookType");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void HeadersCanBeSetToEmptyDictionary()
    {
        // Arrange
        var config = new WebhookConfiguration();
        var emptyHeaders = new Dictionary<string, string>();

        // Act
        config.Headers = emptyHeaders;

        // Assert
        config.Headers.ShouldNotBeNull();
        config.Headers.Count.ShouldBe(0);
    }
}
