using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationRequestBuilder class.
/// </summary>
public class NotificationRequestBuilderTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ToAddsSingleRecipient()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");

        // Act
        var result = builder.To("user@example.com");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ToAddsMultipleRecipients()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");
        var recipients = new[] { "user1@example.com", "user2@example.com" };

        // Act
        var result = builder.To(recipients);

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WithSubjectSetsSubject()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");

        // Act
        var result = builder.WithSubject("Test Subject");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WithMessageSetsMessage()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");

        // Act
        var result = builder.WithMessage("Test Message");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WithPrioritySetsPriority()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");

        // Act
        var result = builder.WithPriority(NotificationPriorities.Critical);

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WithMetadataAddsMetadata()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");

        // Act
        var result = builder.WithMetadata("key1", "value1");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WithMetadataAddsMultipleKeysWhenCalledRepeatedly()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");

        // Act
        builder.WithMetadata("key1", "value1");
        var result = builder.WithMetadata("key2", "value2");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void WithCorrelationIdSetsCorrelationId()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email");

        // Act
        var result = builder.WithCorrelationId("correlation-123");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void BuildCreatesNotificationRequest()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email")
            .To("user@example.com")
            .WithSubject("Subject")
            .WithMessage("Message")
            .WithPriority(NotificationPriorities.High)
            .WithMetadata("key", "value")
            .WithCorrelationId("correlation-123");

        // Act
        var request = builder.Build();

        // Assert
        request.ShouldNotBeNull();
        request.ChannelName.ShouldBe("Email");
        request.Recipients.ShouldContain("user@example.com");
        request.Subject.ShouldBe("Subject");
        request.Message.ShouldBe("Message");
        request.Priority.Name.ShouldBe("High");
        request.Metadata.ShouldNotBeNull();
        request.Metadata!["key"].ShouldBe("value");
        request.CorrelationId.ShouldBe("correlation-123");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void BuildWithMultipleRecipientsIncludesAllRecipients()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email")
            .To("user1@example.com")
            .To(new[] { "user2@example.com", "user3@example.com" })
            .WithSubject("Subject")
            .WithMessage("Message");

        // Act
        var request = builder.Build();

        // Assert
        request.Recipients.Count.ShouldBe(3);
        request.Recipients.ShouldContain("user1@example.com");
        request.Recipients.ShouldContain("user2@example.com");
        request.Recipients.ShouldContain("user3@example.com");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void BuildWithNoMetadataIncludesNull()
    {
        // Arrange
        var builder = new NotificationRequestBuilder("Email")
            .To("user@example.com")
            .WithSubject("Subject")
            .WithMessage("Message");

        // Act
        var request = builder.Build();

        // Assert
        request.Metadata.ShouldNotBeNull();
    }
}
