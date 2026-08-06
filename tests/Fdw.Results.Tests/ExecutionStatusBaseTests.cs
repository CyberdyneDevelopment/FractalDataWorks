using Fdw.Results.ExecutionStatus;
using Shouldly;
using Xunit;

namespace Fdw.Results.Tests;

/// <summary>
/// Tests for <see cref="ExecutionStatusBase"/> covering constructor and property behavior.
/// Uses concrete derived types directly since they have RestrictToCurrentCompilation.
/// </summary>
public sealed class ExecutionStatusBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void PendingStatusSetsAllProperties()
    {
        // Arrange & Act
        var status = new PendingStatus();

        // Assert
        status.Id.ShouldBe(1);
        status.Name.ShouldBe("Pending");
        status.Icon.ShouldBe(PendingStatus.PendingIcon);
        status.Color.ShouldBe("Default");
        status.IsTerminal.ShouldBeFalse();
        status.IsSuccess.ShouldBeFalse();
        status.IsInProgress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RunningStatusSetsIsInProgressTrue()
    {
        // Arrange & Act
        var status = new RunningStatus();

        // Assert
        status.Id.ShouldBe(2);
        status.Name.ShouldBe("Running");
        status.Icon.ShouldBe(RunningStatus.RunningIcon);
        status.Color.ShouldBe("Info");
        status.IsTerminal.ShouldBeFalse();
        status.IsSuccess.ShouldBeFalse();
        status.IsInProgress.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SucceededStatusSetsIsTerminalAndIsSuccessTrue()
    {
        // Arrange & Act
        var status = new SucceededStatus();

        // Assert
        status.Id.ShouldBe(3);
        status.Name.ShouldBe("Succeeded");
        status.Icon.ShouldBe(SucceededStatus.SucceededIcon);
        status.Color.ShouldBe("Success");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeTrue();
        status.IsInProgress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailedStatusSetsIsTerminalTrueAndIsSuccessFalse()
    {
        // Arrange & Act
        var status = new FailedStatus();

        // Assert
        status.Id.ShouldBe(4);
        status.Name.ShouldBe("Failed");
        status.Icon.ShouldBe(FailedStatus.FailedIcon);
        status.Color.ShouldBe("Error");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeFalse();
        status.IsInProgress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CancelledStatusSetsCorrectProperties()
    {
        // Arrange & Act
        var status = new CancelledStatus();

        // Assert
        status.Id.ShouldBe(5);
        status.Name.ShouldBe("Cancelled");
        status.Icon.ShouldBe(CancelledStatus.CancelledIcon);
        status.Color.ShouldBe("Warning");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeFalse();
        status.IsInProgress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SkippedStatusSetsIsSuccessTrue()
    {
        // Arrange & Act
        var status = new SkippedStatus();

        // Assert
        status.Id.ShouldBe(6);
        status.Name.ShouldBe("Skipped");
        status.Icon.ShouldBe(SkippedStatus.SkippedIcon);
        status.Color.ShouldBe("Default");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeTrue();
        status.IsInProgress.ShouldBeFalse();
    }
}
