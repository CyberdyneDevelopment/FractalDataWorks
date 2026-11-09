using System.Linq;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>Tests for the phase state and phase collections (FDW-4149): the names are the vocabulary the lifecycle speaks, and each state says what it means.</summary>
public sealed class PhaseStatesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Services")]
    public void TheStatesAreNotRunDeferredAndRan()
    {
        PhaseStates.All().Select(state => state.Name).OrderBy(name => name, System.StringComparer.Ordinal)
            .ShouldBe(["Deferred", "NotRun", "Ran"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Services")]
    public void OnlyRanHasRunAndOnlyDeferredIsDeferred()
    {
        PhaseStates.NotRun.HasRun.ShouldBeFalse();
        PhaseStates.NotRun.IsDeferred.ShouldBeFalse();
        PhaseStates.Deferred.HasRun.ShouldBeFalse();
        PhaseStates.Deferred.IsDeferred.ShouldBeTrue();
        PhaseStates.Ran.HasRun.ShouldBeTrue();
        PhaseStates.Ran.IsDeferred.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Services")]
    public void ThePhasesAreConfigureRegisterAndInitialize()
    {
        ServiceTypePhases.All().Select(phase => phase.Name).OrderBy(name => name, System.StringComparer.Ordinal)
            .ShouldBe(["Configure", "Initialize", "Register"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Services")]
    public void ANameThatIsNotAStateOrAPhaseAnswersTheNotFoundSentinel()
    {
        ReferenceEquals(PhaseStates.ByName("Paused"), PhaseStates.NotFound).ShouldBeTrue();
        ReferenceEquals(ServiceTypePhases.ByName("Teardown"), ServiceTypePhases.NotFound).ShouldBeTrue();
    }
}
