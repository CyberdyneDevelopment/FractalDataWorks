using Fdw.Services.Scheduling.Abstractions.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

public class TriggerTests
{
    #region CreateCron Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronReturnsValidTrigger()
    {
        // Act
        var trigger = Trigger.CreateCron(
            name: "Daily Backup",
            cronExpression: "0 2 * * *",
            timeZoneId: "UTC"
        );

        // Assert
        trigger.ShouldNotBeNull();
        trigger.TriggerName.ShouldBe("Daily Backup");
        trigger.TriggerType.ShouldBe("Cron");
        trigger.IsEnabled.ShouldBeTrue();
        trigger.Configuration.ShouldContainKey("CronExpression");
        trigger.Configuration["CronExpression"].ShouldBe("0 2 * * *");
        trigger.Configuration.ShouldContainKey("TimeZoneId");
        trigger.Configuration["TimeZoneId"].ShouldBe("UTC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronWithoutTimeZoneUsesUtc()
    {
        // Act
        var trigger = Trigger.CreateCron(
            name: "Test Trigger",
            cronExpression: "0 9 * * *"
        );

        // Assert
        trigger.Configuration.ShouldNotContainKey("TimeZoneId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronWithDescriptionSetsMetadata()
    {
        // Act
        var trigger = Trigger.CreateCron(
            name: "Test Trigger",
            cronExpression: "0 9 * * *",
            description: "Test description"
        );

        // Assert
        trigger.Description.ShouldBe("Test description");
        trigger.Metadata.ShouldNotBeNull();
        trigger.Metadata.ShouldContainKey("Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronWithDisabledStatusCreatesDisabledTrigger()
    {
        // Act
        var trigger = Trigger.CreateCron(
            name: "Test Trigger",
            cronExpression: "0 9 * * *",
            isEnabled: false
        );

        // Assert
        trigger.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(
                name: null!,
                cronExpression: "0 9 * * *"
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronThrowsWhenNameIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(
                name: "",
                cronExpression: "0 9 * * *"
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronThrowsWhenCronExpressionIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(
                name: "Test",
                cronExpression: null!
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronThrowsWhenCronExpressionIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateCron(
                name: "Test",
                cronExpression: ""
            ));
    }

    #endregion

    #region CreateInterval Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalReturnsValidTrigger()
    {
        // Act
        var trigger = Trigger.CreateInterval(
            name: "Health Check",
            intervalMinutes: 30
        );

        // Assert
        trigger.ShouldNotBeNull();
        trigger.TriggerName.ShouldBe("Health Check");
        trigger.TriggerType.ShouldBe("Interval");
        trigger.IsEnabled.ShouldBeTrue();
        trigger.Configuration.ShouldContainKey("IntervalMinutes");
        trigger.Configuration["IntervalMinutes"].ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalWithStartDelayAddsStartTime()
    {
        // Act
        var beforeCreation = DateTime.UtcNow;
        var trigger = Trigger.CreateInterval(
            name: "Test Trigger",
            intervalMinutes: 60,
            startDelayMinutes: 5
        );
        var afterCreation = DateTime.UtcNow;

        // Assert
        trigger.Configuration.ShouldContainKey("StartTime");
        var startTime = (DateTime)trigger.Configuration["StartTime"];
        startTime.ShouldBeGreaterThan(beforeCreation.AddMinutes(4));
        startTime.ShouldBeLessThan(afterCreation.AddMinutes(6));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateInterval(
                name: null!,
                intervalMinutes: 30
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalThrowsWhenIntervalIsZero()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateInterval(
                name: "Test",
                intervalMinutes: 0
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalThrowsWhenIntervalIsNegative()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateInterval(
                name: "Test",
                intervalMinutes: -10
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalThrowsWhenStartDelayIsNegative()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateInterval(
                name: "Test",
                intervalMinutes: 30,
                startDelayMinutes: -5
            ));
    }

    #endregion

    #region CreateOnce Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceReturnsValidTrigger()
    {
        // Arrange
        var executeAt = DateTime.UtcNow.AddHours(2);

        // Act
        var trigger = Trigger.CreateOnce(
            name: "Maintenance Window",
            executeAtUtc: executeAt
        );

        // Assert
        trigger.ShouldNotBeNull();
        trigger.TriggerName.ShouldBe("Maintenance Window");
        trigger.TriggerType.ShouldBe("Once");
        trigger.IsEnabled.ShouldBeTrue();
        trigger.Configuration.ShouldContainKey("StartTime");
        trigger.Configuration["StartTime"].ShouldBe(executeAt);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateOnce(
                name: null!,
                executeAtUtc: DateTime.UtcNow.AddHours(1)
            ));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceThrowsWhenExecuteTimeIsNotUtc()
    {
        // Arrange
        var localTime = DateTime.Now;

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateOnce(
                name: "Test",
                executeAtUtc: localTime
            ));
    }

    #endregion

    #region CreateManual Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualReturnsValidTrigger()
    {
        // Act
        var trigger = Trigger.CreateManual(
            name: "Manual Backup"
        );

        // Assert
        trigger.ShouldNotBeNull();
        trigger.TriggerName.ShouldBe("Manual Backup");
        trigger.TriggerType.ShouldBe("Manual");
        trigger.IsEnabled.ShouldBeTrue();
        trigger.Configuration.ShouldContainKey("AllowConcurrent");
        trigger.Configuration["AllowConcurrent"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualWithAllowConcurrentFalseSetsConfiguration()
    {
        // Act
        var trigger = Trigger.CreateManual(
            name: "Test Trigger",
            allowConcurrent: false
        );

        // Assert
        trigger.Configuration["AllowConcurrent"].ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualWithRequiredRoleSetsConfiguration()
    {
        // Act
        var trigger = Trigger.CreateManual(
            name: "Test Trigger",
            requiredRole: "Administrator"
        );

        // Assert
        trigger.Configuration.ShouldContainKey("RequiredRole");
        trigger.Configuration["RequiredRole"].ShouldBe("Administrator");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualWithDescriptionSetsConfiguration()
    {
        // Act
        var trigger = Trigger.CreateManual(
            name: "Test Trigger",
            description: "Test description"
        );

        // Assert
        trigger.Configuration.ShouldContainKey("Description");
        trigger.Configuration["Description"].ShouldBe("Test description");
        trigger.Description.ShouldBe("Test description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateManual(name: null!));
    }

    #endregion

    #region Validate Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateReturnsSuccessForValidCronTrigger()
    {
        // Arrange
        var trigger = Trigger.CreateCron(
            name: "Test",
            cronExpression: "0 9 * * *"
        );

        // Act
        var result = trigger.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateReturnsSuccessForValidIntervalTrigger()
    {
        // Arrange
        var trigger = Trigger.CreateInterval(
            name: "Test",
            intervalMinutes: 30
        );

        // Act
        var result = trigger.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateReturnsSuccessForValidOnceTrigger()
    {
        // Arrange
        var trigger = Trigger.CreateOnce(
            name: "Test",
            executeAtUtc: DateTime.UtcNow.AddHours(1)
        );

        // Act
        var result = trigger.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ValidateReturnsSuccessForValidManualTrigger()
    {
        // Arrange
        var trigger = Trigger.CreateManual(name: "Test");

        // Act
        var result = trigger.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region State Management Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void UpdateEnabledStatusChangesStatus()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", isEnabled: true);
        var originalModified = trigger.ModifiedUtc;

        // Wait to ensure timestamp changes
        Thread.Sleep(1);

        // Act
        trigger.UpdateEnabledStatus(false);

        // Assert
        trigger.IsEnabled.ShouldBeFalse();
        trigger.ModifiedUtc.ShouldBeGreaterThan(originalModified);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void SetMetadataAddsMetadata()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        var originalModified = trigger.ModifiedUtc;

        // Wait to ensure timestamp changes
        Thread.Sleep(1);

        // Act
        trigger.SetMetadata("CustomKey", "CustomValue");

        // Assert
        trigger.Metadata.ShouldNotBeNull();
        trigger.Metadata.ShouldContainKey("CustomKey");
        trigger.Metadata["CustomKey"].ShouldBe("CustomValue");
        trigger.ModifiedUtc.ShouldBeGreaterThan(originalModified);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void SetMetadataThrowsWhenKeyIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            trigger.SetMetadata(null!, "value"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataRemovesExistingMetadata()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", description: "Desc");
        var originalModified = trigger.ModifiedUtc;

        // Wait to ensure timestamp changes
        Thread.Sleep(1);

        // Act
        var removed = trigger.RemoveMetadata("Description");

        // Assert
        removed.ShouldBeTrue();
        if (trigger.Metadata != null)
        {
            trigger.Metadata.ShouldNotContainKey("Description");
        }
        trigger.ModifiedUtc.ShouldBeGreaterThan(originalModified);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataReturnsFalseForNonexistentKey()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act
        var removed = trigger.RemoveMetadata("NonexistentKey");

        // Assert
        removed.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataThrowsWhenKeyIsNull()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            trigger.RemoveMetadata(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataDoesNotUpdateTimestampWhenKeyNotFound()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        var originalModified = trigger.ModifiedUtc;

        // Act
        trigger.RemoveMetadata("NonexistentKey");

        // Assert
        trigger.ModifiedUtc.ShouldBe(originalModified);
    }

    #endregion

    #region Object Overrides Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ToStringReturnsFormattedString()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test Trigger", "0 9 * * *");

        // Act
        var result = trigger.ToString();

        // Assert
        result.ShouldContain("Test Trigger");
        result.ShouldContain("Cron");
        result.ShouldContain("Enabled");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForDifferentTriggerIds()
    {
        // Arrange
        var trigger1 = Trigger.CreateCron("Test 1", "0 9 * * *");
        var trigger2 = Trigger.CreateCron("Test 2", "0 9 * * *");

        // Act & Assert
        trigger1.Equals(trigger2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsTrueForSameTrigger()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        trigger.Equals(trigger).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void GetHashCodeIsConsistentForSameTrigger()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act
        var hash1 = trigger.GetHashCode();
        var hash2 = trigger.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    #endregion

    #region Property Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void IdPropertyReturnsTriggerId()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        trigger.Id.ShouldBe(trigger.TriggerId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void NamePropertyReturnsTriggerName()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        trigger.Name.ShouldBe(trigger.TriggerName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void MetadataReturnsNullWhenEmpty()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Remove any default metadata
        if (trigger.Metadata != null)
        {
            foreach (var key in trigger.Metadata.Keys.ToList())
            {
                trigger.RemoveMetadata(key);
            }
        }

        // Act & Assert
        trigger.Metadata.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void DescriptionReturnsNullWhenNotSet()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");

        // Act & Assert
        trigger.Description.ShouldBeNull();
    }

    #endregion
}
