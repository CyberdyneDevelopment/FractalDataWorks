using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationChannelBase class.
/// </summary>
public class NotificationChannelBaseTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestChannel : NotificationChannelBase
    {
        public TestChannel(
            int id,
            string name,
            string description,
            bool supportsBatchSend = true,
            bool supportsRichContent = true,
            bool supportsAttachments = false,
            int? maxMessageLength = null)
            : base(id, name, description, supportsBatchSend, supportsRichContent, supportsAttachments, maxMessageLength)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsAllProperties()
    {
        // Arrange & Act
        var channel = new TestChannel(
            id: 1,
            name: "TestChannel",
            description: "Test channel description",
            supportsBatchSend: true,
            supportsRichContent: false,
            supportsAttachments: true,
            maxMessageLength: 5000);

        // Assert
        channel.Id.ShouldBe(1);
        channel.Name.ShouldBe("TestChannel");
        channel.Description.ShouldBe("Test channel description");
        channel.SupportsBatchSend.ShouldBeTrue();
        channel.SupportsRichContent.ShouldBeFalse();
        channel.SupportsAttachments.ShouldBeTrue();
        channel.MaxMessageLength.ShouldBe(5000);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithDefaultsUsesExpectedDefaults()
    {
        // Arrange & Act
        var channel = new TestChannel(
            id: 2,
            name: "DefaultChannel",
            description: "Default test channel");

        // Assert
        channel.Id.ShouldBe(2);
        channel.Name.ShouldBe("DefaultChannel");
        channel.SupportsBatchSend.ShouldBeTrue();
        channel.SupportsRichContent.ShouldBeTrue();
        channel.SupportsAttachments.ShouldBeFalse();
        channel.MaxMessageLength.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EmailChannelHasCorrectProperties()
    {
        // Arrange & Act
        var channel = new EmailChannel();

        // Assert
        channel.Id.ShouldBe(1);
        channel.Name.ShouldBe("Email");
        channel.Description.ShouldBe("SMTP email notifications");
        channel.SupportsBatchSend.ShouldBeTrue();
        channel.SupportsRichContent.ShouldBeTrue();
        channel.SupportsAttachments.ShouldBeTrue();
        channel.MaxMessageLength.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void TeamsChannelHasCorrectProperties()
    {
        // Arrange & Act
        var channel = new TeamsChannel();

        // Assert
        channel.Id.ShouldBe(2);
        channel.Name.ShouldBe("Teams");
        channel.Description.ShouldBe("Microsoft Teams webhook notifications");
        channel.SupportsBatchSend.ShouldBeFalse();
        channel.SupportsRichContent.ShouldBeTrue();
        channel.SupportsAttachments.ShouldBeFalse();
        channel.MaxMessageLength.ShouldBe(28000);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WebhookChannelHasCorrectProperties()
    {
        // Arrange & Act
        var channel = new WebhookChannel();

        // Assert
        channel.Id.ShouldBe(3);
        channel.Name.ShouldBe("Webhook");
        channel.Description.ShouldBe("Generic HTTP webhook notifications");
        channel.SupportsBatchSend.ShouldBeTrue();
        channel.SupportsRichContent.ShouldBeTrue();
        channel.SupportsAttachments.ShouldBeFalse();
        channel.MaxMessageLength.ShouldBeNull();
    }
}
