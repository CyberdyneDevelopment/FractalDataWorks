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
            string name,
            string description,
            bool supportsBatchSend = true,
            bool supportsRichContent = true,
            bool supportsAttachments = false,
            int? maxMessageLength = null)
            : base(name, description, supportsBatchSend, supportsRichContent, supportsAttachments, maxMessageLength)
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
            name: "TestChannel",
            description: "Test channel description",
            supportsBatchSend: true,
            supportsRichContent: false,
            supportsAttachments: true,
            maxMessageLength: 5000);
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
            name: "DefaultChannel",
            description: "Default test channel");
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
    public void WebhookChannelHasCorrectProperties()
    {
        // Arrange & Act
        var channel = new WebhookChannel();
        channel.Name.ShouldBe("Webhook");
        channel.Description.ShouldBe("Generic HTTP webhook notifications");
        channel.SupportsBatchSend.ShouldBeTrue();
        channel.SupportsRichContent.ShouldBeTrue();
        channel.SupportsAttachments.ShouldBeFalse();
        channel.MaxMessageLength.ShouldBeNull();
    }
}
