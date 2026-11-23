using System;
using System.Linq;
using Fdw.Services.Etl.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-6519: what a run's record says about where the run came from. The host and the slot are the process's (the platform's application name and the one
/// reader of the slot file), the person and the run mode are the run's; a run whose row could not state the mode or the host is refused with a code, and absence
/// (a tenant run has no person, a host off a slot has no slot) is recorded as absence.
/// </summary>
[Trait("Category", "RunRecord")]
public sealed class PipelineRunOriginsTests
{
    private static PipelineRunOrigins Origins(string application = "Reference.Etl.Server", string? slot = "a")
        => TestRunOrigins.Create(application, slot);

    [Fact]
    [Trait("Priority", "P0")]
    public void ARunForAPersonStatesThePersonTheModeTheHostAndTheSlot()
    {
        var person = Guid.NewGuid();

        var made = Origins().Of(person, "Queued");

        made.IsSuccess.ShouldBeTrue();
        var origin = made.Value.ShouldNotBeNull();
        origin.ActingPersonId.ShouldBe(person);
        origin.RunMode.ShouldBe("Queued");
        origin.ExecutingHost.ShouldBe("Reference.Etl.Server");
        origin.ExecutingSlot.ShouldBe("a");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ATenantRunStatesNoPersonAndAHostOffASlotStatesNoSlot()
    {
        var made = Origins(slot: null).Of(null, "Queued");

        made.IsSuccess.ShouldBeTrue();
        made.Value.ShouldNotBeNull().ActingPersonId.ShouldBeNull();
        made.Value.ExecutingSlot.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [Trait("Priority", "P0")]
    public void ARunWithNoModeIsRefusedWithACode(string? runMode)
    {
        var made = Origins().Of(Guid.NewGuid(), runMode);

        made.IsFailure.ShouldBeTrue();
        made.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-60842");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AHostThatStatesNoApplicationNameIsRefusedWithACode()
    {
        var made = Origins(application: " ").Of(Guid.NewGuid(), "Queued");

        made.IsFailure.ShouldBeTrue();
        made.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-60842");
    }
}
