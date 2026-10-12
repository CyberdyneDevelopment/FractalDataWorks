using Fdw.Services.Notifications.Abstractions;
using Fdw.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationRequest class.
/// </summary>
public class NotificationRequestTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsAllProperties()
    {
        // Arrange
        var channelName = "Email";
        var recipients = new List<string> { "user@example.com" };
        var subject = "Test Subject";
        var message = "Test Message";
        var priority = NotificationPriorities.High;
        var metadata = new Dictionary<string, object?> { ["key"] = "value" };
        var correlationId = "correlation-123";

        // Act
        var request = new NotificationRequest(
            channelName,
            recipients,
            subject,
            message,
            priority,
            metadata,
            correlationId);

        // Assert
        request.ChannelName.ShouldBe(channelName);
        request.Recipients.ShouldBe(recipients);
        request.Subject.ShouldBe(subject);
        request.Message.ShouldBe(message);
        request.Priority.Name.ShouldBe("High");
        request.Metadata.ShouldBe(metadata);
        request.CorrelationId.ShouldBe(correlationId);
        request.CommandId.ShouldNotBe(Guid.Empty);
        request.RequestId.ShouldBe(request.CommandId.ToString());
        request.CommandType.ShouldBe("Notification");
        request.Category.ShouldBe("Notifications");
        request.CreatedAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithDefaultsUsesNormalPriority()
    {
        // Arrange
        var channelName = "Email";
        var recipients = new List<string> { "user@example.com" };
        var subject = "Test";
        var message = "Message";

        // Act
        var request = new NotificationRequest(channelName, recipients, subject, message);

        // Assert
        request.Priority.Name.ShouldBe("Normal");
        request.Metadata.ShouldBeNull();
        request.CorrelationId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void IGenericCommandCreatedAtReturnsDateTime()
    {
        // Arrange
        var request = new NotificationRequest(
            "Email",
            new List<string> { "user@example.com" },
            "Subject",
            "Message");

        // Act
        var createdAt = ((IGenericCommand)request).CreatedAt;

        // Assert
        createdAt.ShouldBe(request.CreatedAt.DateTime);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void CreateReturnsBuilder()
    {
        // Arrange & Act
        var builder = NotificationRequest.Create("Email");

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<NotificationRequestBuilder>();
    }
}
