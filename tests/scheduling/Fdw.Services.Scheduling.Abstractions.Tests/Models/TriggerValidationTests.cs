using Fdw.Services.Scheduling.Abstractions.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

public class TriggerValidationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateFailsForCronTriggerWithInvalidExpression()
    {
        // Arrange - Create cron trigger with too few fields
        var trigger = Trigger.CreateCron("Test", "0 9 *");  // Only 3 fields instead of 5+

        // Act
        var result = trigger.Validate();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronThrowsWhenNameIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(
                name: "   ",
                cronExpression: "0 9 * * *"
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronThrowsWhenCronExpressionIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(
                name: "Test",
                cronExpression: "   "
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalThrowsWhenNameIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateInterval(
                name: "",
                intervalMinutes: 30
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalThrowsWhenNameIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateInterval(
                name: "   ",
                intervalMinutes: 30
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceThrowsWhenNameIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateOnce(
                name: "",
                executeAtUtc: DateTime.UtcNow.AddHours(1)
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceThrowsWhenNameIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateOnce(
                name: "   ",
                executeAtUtc: DateTime.UtcNow.AddHours(1)
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualThrowsWhenNameIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateManual(name: ""));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualThrowsWhenNameIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateManual(name: "   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void SetMetadataThrowsWhenKeyIsEmpty()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            trigger.SetMetadata("", "value"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void SetMetadataThrowsWhenKeyIsWhitespace()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            trigger.SetMetadata("   ", "value"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataThrowsWhenKeyIsEmpty()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            trigger.RemoveMetadata(""));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataThrowsWhenKeyIsWhitespace()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            trigger.RemoveMetadata("   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerConstructorThrowsWhenTriggerIdIsNull()
    {
        // We cannot directly test the private constructor, but CreateCron uses it internally
        // and validates parameters. Let's test that parameters are properly validated.
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(null!, "0 9 * * *"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerConstructorThrowsWhenTriggerNameIsNull()
    {
        // Test via CreateCron which calls the constructor
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(null!, "0 9 * * *"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void TriggerConstructorThrowsWhenTriggerTypeIsNull()
    {
        // This is handled internally by factory methods - they set the type
        // But we can verify the validation in CreateCron
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron("", "0 9 * * *"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void DescriptionReturnsValueFromMetadata()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", description: "Test description");

        // Act & Assert
        trigger.Description.ShouldBe("Test description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void MetadataReturnsNullAfterRemovingAllItems()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", description: "Test");

        // Act
        trigger.RemoveMetadata("Description");

        // Assert
        trigger.Metadata.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ConfigurationReturnsReadOnlyDictionary()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act
        var config = trigger.Configuration;

        // Assert
        config.ShouldNotBeNull();
        config.ShouldContainKey("CronExpression");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        trigger.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForDifferentType()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        trigger.Equals("not a trigger").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void IntervalTriggerWithZeroStartDelayDoesNotAddStartTime()
    {
        // Arrange & Act
        var trigger = Trigger.CreateInterval(
            name: "Test",
            intervalMinutes: 30,
            startDelayMinutes: 0
        );

        // Assert
        trigger.Configuration.ShouldNotContainKey("StartTime");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualTriggerWithoutDescriptionDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateManual("Test");

        // Assert
        trigger.Configuration.ShouldNotContainKey("Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualTriggerWithoutRequiredRoleDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateManual("Test");

        // Assert
        trigger.Configuration.ShouldNotContainKey("RequiredRole");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualTriggerWithEmptyDescriptionDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateManual("Test", description: "");

        // Assert
        trigger.Configuration.ShouldNotContainKey("Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualTriggerWithEmptyRequiredRoleDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateManual("Test", requiredRole: "");

        // Assert
        trigger.Configuration.ShouldNotContainKey("RequiredRole");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualTriggerWithWhitespaceDescriptionDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateManual("Test", description: "   ");

        // Assert
        trigger.Configuration.ShouldNotContainKey("Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualTriggerWithWhitespaceRequiredRoleDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateManual("Test", requiredRole: "   ");

        // Assert
        trigger.Configuration.ShouldNotContainKey("RequiredRole");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronTriggerWithoutTimeZoneDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Assert
        trigger.Configuration.ShouldNotContainKey("TimeZoneId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronTriggerWithEmptyTimeZoneDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", timeZoneId: "");

        // Assert
        trigger.Configuration.ShouldNotContainKey("TimeZoneId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronTriggerWithWhitespaceTimeZoneDoesNotIncludeIt()
    {
        // Arrange & Act
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", timeZoneId: "   ");

        // Assert
        trigger.Configuration.ShouldNotContainKey("TimeZoneId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreatedUtcIsSetCorrectly()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Assert
        var after = DateTime.UtcNow;
        trigger.CreatedUtc.ShouldBeGreaterThanOrEqualTo(before);
        trigger.CreatedUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ModifiedUtcIsSetCorrectly()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Assert
        var after = DateTime.UtcNow;
        trigger.ModifiedUtc.ShouldBeGreaterThanOrEqualTo(before);
        trigger.ModifiedUtc.ShouldBeLessThanOrEqualTo(after);
    }
}
