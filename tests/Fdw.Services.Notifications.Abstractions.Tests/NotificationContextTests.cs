using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationContext class.
/// </summary>
public class NotificationContextTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ExecutionStatusCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.ExecutionStatus = "Failed";

        // Assert
        context.ExecutionStatus.ShouldBe("Failed");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void RetryCountCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.RetryCount = 5;

        // Assert
        context.RetryCount.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConsecutiveFailuresCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.ConsecutiveFailures = 10;

        // Assert
        context.ConsecutiveFailures.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DurationCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();
        var duration = TimeSpan.FromMinutes(5);

        // Act
        context.Duration = duration;

        // Assert
        context.Duration.ShouldBe(duration);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ExpectedDurationCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();
        var expectedDuration = TimeSpan.FromMinutes(3);

        // Act
        context.ExpectedDuration = expectedDuration;

        // Assert
        context.ExpectedDuration.ShouldBe(expectedDuration);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ThresholdCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.Threshold = 20;

        // Assert
        context.Threshold.ShouldBe(20);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DurationTicksCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();
        var ticks = TimeSpan.FromHours(1).Ticks;

        // Act
        context.DurationTicks = ticks;

        // Assert
        context.DurationTicks.ShouldBe(ticks);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void FieldCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.Field = "Status";

        // Assert
        context.Field.ShouldBe("Status");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void OperatorCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.Operator = "Equal";

        // Assert
        context.Operator.ShouldBe("Equal");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ValueCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.Value = "Failed";

        // Assert
        context.Value.ShouldBe("Failed");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ActualValueCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.ActualValue = "Succeeded";

        // Assert
        context.ActualValue.ShouldBe("Succeeded");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void IsNegatedCanBeSetAndRetrieved()
    {
        // Arrange
        var context = new NotificationContext();

        // Act
        context.IsNegated = true;

        // Assert
        context.IsNegated.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DefaultValuesAreExpected()
    {
        // Arrange & Act
        var context = new NotificationContext();

        // Assert
        context.ExecutionStatus.ShouldBeNull();
        context.RetryCount.ShouldBe(0);
        context.ConsecutiveFailures.ShouldBe(0);
        context.Duration.ShouldBe(TimeSpan.Zero);
        context.ExpectedDuration.ShouldBeNull();
        context.Threshold.ShouldBeNull();
        context.DurationTicks.ShouldBeNull();
        context.Field.ShouldBeNull();
        context.Operator.ShouldBeNull();
        context.Value.ShouldBeNull();
        context.ActualValue.ShouldBeNull();
        context.IsNegated.ShouldBeFalse();
    }
}
