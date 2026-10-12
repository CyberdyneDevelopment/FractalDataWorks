using Fdw.Services.Scheduling.Abstractions.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

public class TriggerAdditionalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronWithMetadataIncludesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["Owner"] = "Admin",
            ["Priority"] = 5
        };

        // Act
        var trigger = Trigger.CreateCron(
            name: "Test",
            cronExpression: "0 9 * * *",
            metadata: metadata
        );

        // Assert
        trigger.Metadata.ShouldNotBeNull();
        trigger.Metadata.ShouldContainKey("Owner");
        trigger.Metadata["Owner"].ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalWithDescriptionSetsMetadata()
    {
        // Act
        var trigger = Trigger.CreateInterval(
            name: "Test",
            intervalMinutes: 30,
            description: "Health check every 30 minutes"
        );

        // Assert
        trigger.Description.ShouldBe("Health check every 30 minutes");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceWithDescriptionSetsMetadata()
    {
        // Act
        var trigger = Trigger.CreateOnce(
            name: "Test",
            executeAtUtc: DateTime.UtcNow.AddHours(2),
            description: "One-time maintenance"
        );

        // Assert
        trigger.Description.ShouldBe("One-time maintenance");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualThrowsWhenNameIsWhitespace()
    {
        Should.Throw<ArgumentException>(() => Trigger.CreateManual(name: "   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateCronThrowsWhenCronExpressionIsWhitespace()
    {
        Should.Throw<ArgumentException>(() => Trigger.CreateCron(name: "Test", cronExpression: "   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalThrowsWhenNameIsEmpty()
    {
        Should.Throw<ArgumentException>(() => Trigger.CreateInterval(name: "", intervalMinutes: 30));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceThrowsWhenNameIsEmpty()
    {
        Should.Throw<ArgumentException>(() =>
            Trigger.CreateOnce(name: "", executeAtUtc: DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void SetMetadataUpdatesExistingKey()
    {
        // Arrange
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", description: "Initial");

        // Act
        trigger.SetMetadata("Description", "Updated");

        // Assert
        trigger.Description.ShouldBe("Updated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void SetMetadataThrowsWhenKeyIsEmpty()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        Should.Throw<ArgumentException>(() => trigger.SetMetadata("", "value"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void SetMetadataThrowsWhenKeyIsWhitespace()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        Should.Throw<ArgumentException>(() => trigger.SetMetadata("   ", "value"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataThrowsWhenKeyIsEmpty()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        Should.Throw<ArgumentException>(() => trigger.RemoveMetadata(""));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void RemoveMetadataThrowsWhenKeyIsWhitespace()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        Should.Throw<ArgumentException>(() => trigger.RemoveMetadata("   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForNullObject()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        trigger.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EqualsReturnsFalseForDifferentType()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        trigger.Equals("not a trigger").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ToStringForDisabledTriggerShowsDisabled()
    {
        var trigger = Trigger.CreateCron("Test", "0 9 * * *", isEnabled: false);
        trigger.ToString().ShouldContain("Disabled");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreatedUtcIsSetOnCreation()
    {
        var before = DateTime.UtcNow;
        var trigger = Trigger.CreateCron("Test", "0 9 * * *");
        var after = DateTime.UtcNow;

        trigger.CreatedUtc.ShouldBeGreaterThanOrEqualTo(before);
        trigger.CreatedUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceWithDisabledStatusCreatesDisabledTrigger()
    {
        var trigger = Trigger.CreateOnce(
            name: "Test",
            executeAtUtc: DateTime.UtcNow.AddHours(1),
            isEnabled: false
        );

        trigger.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalWithDisabledStatusCreatesDisabledTrigger()
    {
        var trigger = Trigger.CreateInterval(
            name: "Test",
            intervalMinutes: 30,
            isEnabled: false
        );

        trigger.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualWithDisabledStatusCreatesDisabledTrigger()
    {
        var trigger = Trigger.CreateManual(
            name: "Test",
            isEnabled: false
        );

        trigger.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateManualWithAllOptionsSetsSetsAll()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["Source"] = "Test" };

        // Act
        var trigger = Trigger.CreateManual(
            name: "Full Manual",
            description: "Full description",
            requiredRole: "Admin",
            allowConcurrent: false,
            isEnabled: true,
            metadata: metadata
        );

        // Assert
        trigger.TriggerName.ShouldBe("Full Manual");
        trigger.Configuration["AllowConcurrent"].ShouldBe(false);
        trigger.Configuration["RequiredRole"].ShouldBe("Admin");
        trigger.Configuration["Description"].ShouldBe("Full description");
        trigger.Metadata.ShouldNotBeNull();
        trigger.Metadata.ShouldContainKey("Source");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateOnceWithMetadata()
    {
        var metadata = new Dictionary<string, object> { ["Source"] = "API" };
        var trigger = Trigger.CreateOnce(
            name: "Test",
            executeAtUtc: DateTime.UtcNow.AddHours(1),
            metadata: metadata
        );

        trigger.Metadata.ShouldNotBeNull();
        trigger.Metadata.ShouldContainKey("Source");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateIntervalWithMetadata()
    {
        var metadata = new Dictionary<string, object> { ["Source"] = "API" };
        var trigger = Trigger.CreateInterval(
            name: "Test",
            intervalMinutes: 30,
            metadata: metadata
        );

        trigger.Metadata.ShouldNotBeNull();
        trigger.Metadata.ShouldContainKey("Source");
    }
}
