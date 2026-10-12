using Fdw.Services.Scheduling.Abstractions.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

public class ScheduleTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewReturnsScheduleWithGeneratedIds()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *", "UTC");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger
        );

        // Assert
        schedule.ShouldNotBeNull();
        schedule.Id.ShouldNotBeNullOrWhiteSpace();
        schedule.Name.ShouldBe("Test Schedule");
        schedule.ProcessType.ShouldBe("TestProcess");
        schedule.ProcessConfiguration.ShouldBe(config);
        schedule.Trigger.ShouldBe(trigger);
        schedule.IsActive.ShouldBeTrue();
        schedule.CreatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        schedule.UpdatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewWithInactiveStatusCreatesInactiveSchedule()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            isActive: false
        );

        // Assert
        schedule.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewWithDescriptionSetsDescription()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            description: "Test description"
        );

        // Assert
        schedule.Description.ShouldBe("Test description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewWithMetadataSetsMetadata()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var metadata = new Dictionary<string, object>
        {
            ["Key1"] = "Value1",
            ["Key2"] = 42
        };

        // Act
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            metadata: metadata
        );

        // Assert
        schedule.Metadata.ShouldNotBeNull();
        schedule.Metadata.ShouldContainKey("Key1");
        schedule.Metadata["Key1"].ShouldBe("Value1");
        schedule.Metadata.ShouldContainKey("Key2");
        schedule.Metadata["Key2"].ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewThrowsWhenNameIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Schedule.CreateNew(
                name: null!,
                processType: "TestProcess",
                processConfiguration: config,
                trigger: trigger
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewThrowsWhenNameIsEmpty()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Schedule.CreateNew(
                name: "",
                processType: "TestProcess",
                processConfiguration: config,
                trigger: trigger
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateNewThrowsWhenNameIsWhitespace()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Schedule.CreateNew(
                name: "   ",
                processType: "TestProcess",
                processConfiguration: config,
                trigger: trigger
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateReturnsScheduleWithSpecifiedIds()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var scheduleId = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        // Act
        var schedule = Schedule.Create(
            id: scheduleId,
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            createdAt: createdAt,
            updatedAt: updatedAt
        );

        // Assert
        schedule.Id.ShouldBe(scheduleId);
        schedule.Name.ShouldBe("Test Schedule");
        schedule.ProcessType.ShouldBe("TestProcess");
        schedule.CreatedAt.ShouldBe(createdAt);
        schedule.UpdatedAt.ShouldBe(updatedAt);
        schedule.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateThrowsWhenIdIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Schedule.Create(
                id: null!,
                name: "Test Schedule",
                processType: "TestProcess",
                processConfiguration: config,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateThrowsWhenIdIsEmpty()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Schedule.Create(
                id: "",
                name: "Test Schedule",
                processType: "TestProcess",
                processConfiguration: config,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateThrowsWhenNameIsEmpty()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Schedule.Create(
                id: Guid.NewGuid().ToString(),
                name: "",
                processType: "TestProcess",
                processConfiguration: config,
                trigger: trigger,
                createdAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateReturnsSuccessForValidSchedule()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger
        );

        // Act
        var result = schedule.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void UpdateActiveStatusChangesStatus()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger,
            isActive: true
        );
        var originalUpdatedAt = schedule.UpdatedAt;

        // Wait a brief moment to ensure timestamp changes
        Thread.Sleep(1);

        // Act
        schedule.UpdateActiveStatus(false);

        // Assert
        schedule.IsActive.ShouldBeFalse();
        schedule.UpdatedAt.ShouldBeGreaterThan(originalUpdatedAt);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void UpdateNextExecutionChangesNextExecution()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger
        );
        var nextExecution = DateTime.UtcNow.AddHours(1);
        var originalUpdatedAt = schedule.UpdatedAt;

        // Wait a brief moment to ensure timestamp changes
        Thread.Sleep(1);

        // Act
        schedule.UpdateNextExecution(nextExecution);

        // Assert
        schedule.NextExecution.ShouldBe(nextExecution);
        schedule.UpdatedAt.ShouldBeGreaterThan(originalUpdatedAt);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ToStringReturnsFormattedString()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var schedule = Schedule.CreateNew(
            name: "Test Schedule",
            processType: "TestProcess",
            processConfiguration: config,
            trigger: trigger
        );

        // Act
        var result = schedule.ToString();

        // Assert
        result.ShouldContain("Test Schedule");
        result.ShouldContain("TestProcess");
        result.ShouldContain("Active");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsTrueForSameId()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var id = Guid.NewGuid().ToString();

        var schedule1 = Schedule.Create(
            id: id,
            name: "Schedule 1",
            processType: "Process1",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );

        var schedule2 = Schedule.Create(
            id: id,
            name: "Schedule 2",
            processType: "Process2",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );

        // Act & Assert
        schedule1.Equals(schedule2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForDifferentIds()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };

        var schedule1 = Schedule.CreateNew("Schedule 1", "Process1", config, trigger);
        var schedule2 = Schedule.CreateNew("Schedule 2", "Process2", config, trigger);

        // Act & Assert
        schedule1.Equals(schedule2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void GetHashCodeReturnsSameValueForSameId()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");
        var config = new { Setting = "Value" };
        var id = Guid.NewGuid().ToString();

        var schedule1 = Schedule.Create(
            id: id,
            name: "Schedule 1",
            processType: "Process1",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );

        var schedule2 = Schedule.Create(
            id: id,
            name: "Schedule 2",
            processType: "Process2",
            processConfiguration: config,
            trigger: trigger,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );

        // Act & Assert
        schedule1.GetHashCode().ShouldBe(schedule2.GetHashCode());
    }
}
