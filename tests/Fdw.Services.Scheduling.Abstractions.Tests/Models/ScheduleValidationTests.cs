using Fdw.Services.Scheduling.Abstractions.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

public class ScheduleValidationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateFailsWhenIdIsNull()
    {
        // Arrange - Create schedule with valid trigger but force null Id via Create with null
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        Should.Throw<ArgumentException>(() =>
            Schedule.Create(
                id: null!,
                name: "Test",
                processType: "Process",
                processConfiguration: config,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateFailsWhenIdIsWhitespace()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        Should.Throw<ArgumentException>(() =>
            Schedule.Create(
                id: "   ",
                name: "Test",
                processType: "Process",
                processConfiguration: config,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateFailsWhenNameIsWhitespace()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        Should.Throw<ArgumentException>(() =>
            Schedule.Create(
                id: "test-id",
                name: "   ",
                processType: "Process",
                processConfiguration: config,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewThrowsWhenProcessTypeIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        Should.Throw<ArgumentNullException>(() =>
            Schedule.CreateNew(
                name: "Test",
                processType: null!,
                processConfiguration: config,
                trigger: trigger
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewThrowsWhenProcessConfigurationIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");

        Should.Throw<ArgumentNullException>(() =>
            Schedule.CreateNew(
                name: "Test",
                processType: "Process",
                processConfiguration: null!,
                trigger: trigger
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewThrowsWhenTriggerIsNull()
    {
        // Arrange
        var config = new { Setting = "Value" };

        Should.Throw<ArgumentNullException>(() =>
            Schedule.CreateNew(
                name: "Test",
                processType: "Process",
                processConfiguration: config,
                trigger: null!
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateThrowsWhenProcessTypeIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        Should.Throw<ArgumentNullException>(() =>
            Schedule.Create(
                id: "test-id",
                name: "Test",
                processType: null!,
                processConfiguration: config,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateThrowsWhenProcessConfigurationIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");

        Should.Throw<ArgumentNullException>(() =>
            Schedule.Create(
                id: "test-id",
                name: "Test",
                processType: "Process",
                processConfiguration: null!,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateThrowsWhenTriggerIsNull()
    {
        // Arrange
        var config = new { Setting = "Value" };

        Should.Throw<ArgumentNullException>(() =>
            Schedule.Create(
                id: "test-id",
                name: "Test",
                processType: "Process",
                processConfiguration: config,
                trigger: null!,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ExtractsCronExpressionFromTriggerConfiguration()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test",
            processType: "Process",
            processConfiguration: config,
            trigger: trigger
        );

        // Assert
        schedule.CronExpression.ShouldBe("0 9 * * *");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ExtractsTimeZoneFromTriggerConfiguration()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *", "America/New_York");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test",
            processType: "Process",
            processConfiguration: config,
            trigger: trigger
        );

        // Assert
        schedule.TimeZoneId.ShouldBe("America/New_York");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void DefaultsToManualCronExpressionForNonCronTriggers()
    {
        // Arrange
        var trigger = Trigger.CreateManual("Test Trigger");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test",
            processType: "Process",
            processConfiguration: config,
            trigger: trigger
        );

        // Assert
        schedule.CronExpression.ShouldBe("@manual");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void DefaultsToUtcTimeZoneForTriggersWithoutTimeZone()
    {
        // Arrange
        var trigger = Trigger.CreateManual("Test Trigger");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test",
            processType: "Process",
            processConfiguration: config,
            trigger: trigger
        );

        // Assert
        schedule.TimeZoneId.ShouldBe("UTC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var schedule = Schedule.CreateNew("Test", "Process", config, trigger);

        // Act & Assert
        schedule.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForDifferentType()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var schedule = Schedule.CreateNew("Test", "Process", config, trigger);

        // Act & Assert
        schedule.Equals("not a schedule").ShouldBeFalse();
    }
}
