using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationConditionTypes TypeCollection.
/// </summary>
public class NotificationConditionTypesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsAllConditionTypes()
    {
        // Arrange & Act
        var conditionTypes = NotificationConditionTypes.All();

        // Assert
        conditionTypes.ShouldNotBeEmpty();
        conditionTypes.Count.ShouldBeGreaterThanOrEqualTo(6);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsRetryThresholdCondition()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ById(1);

        // Assert
        condition.ShouldNotBeNull();
        condition.Id.ShouldBe(1);
        condition.Name.ShouldBe("RetryThreshold");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsConsecutiveFailuresCondition()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ById(2);

        // Assert
        condition.ShouldNotBeNull();
        condition.Id.ShouldBe(2);
        condition.Name.ShouldBe("ConsecutiveFailures");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsDurationExceededCondition()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ById(3);

        // Assert
        condition.ShouldNotBeNull();
        condition.Id.ShouldBe(3);
        condition.Name.ShouldBe("DurationExceeded");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsExecutionStatusCondition()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ById(4);

        // Assert
        condition.ShouldNotBeNull();
        condition.Id.ShouldBe(4);
        condition.Name.ShouldBe("ExecutionStatus");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsValueCondition()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ById(5);

        // Assert
        condition.ShouldNotBeNull();
        condition.Id.ShouldBe(5);
        condition.Name.ShouldBe("ValueCondition");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsTimeWindowCondition()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ById(6);

        // Assert
        condition.ShouldNotBeNull();
        condition.Id.ShouldBe(6);
        condition.Name.ShouldBe("TimeWindow");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ById(99999);

        // Assert
        condition.ShouldNotBeNull();
        condition.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsCorrectCondition()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ByName("RetryThreshold");

        // Assert
        condition.ShouldNotBeNull();
        condition.Name.ShouldBe("RetryThreshold");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange & Act
        var lowercase = NotificationConditionTypes.ByName("retrythreshold");

        // Assert
        lowercase.ShouldNotBeNull();
        lowercase.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.ByName("UnknownCondition");

        // Assert
        condition.ShouldNotBeNull();
        condition.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Arrange & Act
        var condition = NotificationConditionTypes.NotFound;

        // Assert
        condition.ShouldNotBeNull();
        condition.Name.ShouldBe("_Empty");
    }
}
