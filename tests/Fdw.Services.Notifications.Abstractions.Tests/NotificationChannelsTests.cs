using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationChannels TypeCollection.
/// </summary>
public class NotificationChannelsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsAllChannels()
    {
        // Arrange & Act
        var channels = NotificationChannels.All();

        // Assert
        channels.ShouldNotBeEmpty();
        channels.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsEmailChannel()
    {
        // Arrange & Act
        var channel = NotificationChannels.ById(1);

        // Assert
        channel.ShouldNotBeNull();
        channel.Id.ShouldBe(1);
        channel.Name.ShouldBe("Email");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsTeamsChannel()
    {
        // Arrange & Act
        var channel = NotificationChannels.ById(2);

        // Assert
        channel.ShouldNotBeNull();
        channel.Id.ShouldBe(2);
        channel.Name.ShouldBe("Teams");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsWebhookChannel()
    {
        // Arrange & Act
        var channel = NotificationChannels.ById(3);

        // Assert
        channel.ShouldNotBeNull();
        channel.Id.ShouldBe(3);
        channel.Name.ShouldBe("Webhook");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Arrange & Act
        var channel = NotificationChannels.ById(99999);

        // Assert
        channel.ShouldNotBeNull();
        channel.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsEmailChannel()
    {
        // Arrange & Act
        var channel = NotificationChannels.ByName("Email");

        // Assert
        channel.ShouldNotBeNull();
        channel.Name.ShouldBe("Email");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsTeamsChannel()
    {
        // Arrange & Act
        var channel = NotificationChannels.ByName("Teams");

        // Assert
        channel.ShouldNotBeNull();
        channel.Name.ShouldBe("Teams");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsWebhookChannel()
    {
        // Arrange & Act
        var channel = NotificationChannels.ByName("Webhook");

        // Assert
        channel.ShouldNotBeNull();
        channel.Name.ShouldBe("Webhook");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange & Act
        var lowercase = NotificationChannels.ByName("email");
        var uppercase = NotificationChannels.ByName("EMAIL");

        // Assert
        lowercase.ShouldNotBeNull();
        lowercase.Name.ShouldBe("_Empty");
        uppercase.ShouldNotBeNull();
        uppercase.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Arrange & Act
        var channel = NotificationChannels.ByName("UnknownChannel");

        // Assert
        channel.ShouldNotBeNull();
        channel.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Arrange & Act
        var channel = NotificationChannels.NotFound;

        // Assert
        channel.ShouldNotBeNull();
        channel.Name.ShouldBe("_Empty");
    }
}
