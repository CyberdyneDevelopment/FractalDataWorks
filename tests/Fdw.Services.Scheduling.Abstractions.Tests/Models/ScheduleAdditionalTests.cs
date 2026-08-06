using Fdw.Services.Scheduling.Abstractions.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

public class ScheduleAdditionalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateWithSpecificProcessIdUsesIt()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var processId = "custom-process-id";

        // Act
        var schedule = Schedule.Create(
            id: Guid.NewGuid().ToString(),
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            processId: processId
        );

        // Assert
        schedule.ProcessId.ShouldBe(processId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateWithNextExecutionSetsNextExecution()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var nextExec = DateTime.UtcNow.AddDays(1);

        // Act
        var schedule = Schedule.Create(
            id: Guid.NewGuid().ToString(),
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            nextExecution: nextExec
        );

        // Assert
        schedule.NextExecution.ShouldBe(nextExec);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateWithInactiveStatusCreatesInactiveSchedule()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.Create(
            id: Guid.NewGuid().ToString(),
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            isActive: false
        );

        // Assert
        schedule.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ScheduleIdMatchesId()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var id = Guid.NewGuid().ToString();

        // Act
        var schedule = Schedule.Create(
            id: id,
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );

        // Assert
        schedule.ScheduleId.ShouldBe(id);
        schedule.ScheduleName.ShouldBe("Test Schedule");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronExpressionExtractedFromTrigger()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *", "America/New_York");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger
        );

        // Assert
        schedule.CronExpression.ShouldBe("0 9 * * *");
        schedule.TimeZoneId.ShouldBe("America/New_York");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualTriggerUsesDefaultCronExpression()
    {
        // Arrange
        var trigger = Trigger.CreateManual("Manual Trigger");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Manual Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger
        );

        // Assert
        schedule.CronExpression.ShouldBe("@manual");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ToStringForInactiveScheduleShowsInactive()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            isActive: false
        );

        // Act
        var result = schedule.ToString();

        // Assert
        result.ShouldContain("Inactive");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForNullObject()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        var schedule = Schedule.CreateNew("Test", "Process", new { }, trigger);

        schedule.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForDifferentType()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        var schedule = Schedule.CreateNew("Test", "Process", new { }, trigger);

        schedule.Equals("not a schedule").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void UpdateNextExecutionClearsWhenNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        var schedule = Schedule.CreateNew("Test", "Process", new { }, trigger);
        schedule.UpdateNextExecution(DateTime.UtcNow.AddHours(1));

        // Act
        schedule.UpdateNextExecution(null);

        // Assert
        schedule.NextExecution.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateWithMetadataSetsMetadata()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1" };

        // Act
        var schedule = Schedule.Create(
            id: Guid.NewGuid().ToString(),
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: new { },
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            metadata: metadata
        );

        // Assert
        schedule.Metadata.ShouldNotBeNull();
        schedule.Metadata.ShouldContainKey("Key1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateFailsWhenUpdatedAtBeforeCreatedAt()
    {
        // Arrange - Create with updatedAt before createdAt to trigger timestamp validation
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddHours(-1);

        var schedule = Schedule.Create(
            id: Guid.NewGuid().ToString(),
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: new { Setting = "Value" },
            trigger: trigger,
            createdAt: createdAt,
            updatedAt: updatedAt
        );

        // Act
        var result = schedule.Validate();

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewWithNullMetadataLeavesMetadataNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: new { },
            trigger: trigger,
            metadata: null
        );

        // Assert
        schedule.Metadata.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateWithDescriptionSetsDescription()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");

        // Act
        var schedule = Schedule.Create(
            id: Guid.NewGuid().ToString(),
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: new { },
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            description: "A test description"
        );

        // Assert
        schedule.Description.ShouldBe("A test description");
    }
}
