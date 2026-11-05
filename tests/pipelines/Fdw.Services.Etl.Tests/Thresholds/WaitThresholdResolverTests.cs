using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Thresholds.WaitThresholdKit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for choosing the owner whose attached thresholds apply to a run (FDW-3161, FDW-3975): the schedule's level when it does not inherit, else the pipeline's, else the
/// server's; and a server that inherits has no level above it and fails.
/// </summary>
public sealed class WaitThresholdResolverTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AScheduleThatDoesNotInheritIsTheOwnerWhateverThePipelineStates()
    {
        var owner = WaitThresholdResolver.Owner(Layer("Server", "Own"), Layer("Pipeline:P", "Own"), Layer("Schedule:S", "None"), NullLogger.Instance);

        owner.IsSuccess.ShouldBeTrue();
        owner.Value!.Owner.ShouldBe("Schedule:S");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void APipelineThatDoesNotInheritIsTheOwnerWhenTheScheduleInheritsOrThereIsNone()
    {
        var withSchedule = WaitThresholdResolver.Owner(Layer("Server", "Own"), Layer("Pipeline:P", "None"), Layer("Schedule:S", "Inherit"), NullLogger.Instance);
        var without = WaitThresholdResolver.Owner(Layer("Server", "Own"), Layer("Pipeline:P", "Own"), null, NullLogger.Instance);

        withSchedule.Value!.Owner.ShouldBe("Pipeline:P");
        without.Value!.Owner.ShouldBe("Pipeline:P");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheServerIsTheOwnerWhenEveryLevelBelowItInherits()
    {
        var owner = WaitThresholdResolver.Owner(Layer("Server", "Own"), Layer("Pipeline:P", "Inherit"), Layer("Schedule:S", "Inherit"), NullLogger.Instance);

        owner.Value!.Owner.ShouldBe("Server");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AServerThatInheritsHasNoLevelAboveItAndFailsNamingTheMode()
    {
        var owner = WaitThresholdResolver.Owner(Layer("Server", "Inherit"), Layer("Pipeline:P", "Inherit"), null, NullLogger.Instance);

        owner.IsFailure.ShouldBeTrue();
        owner.CurrentMessage.ShouldNotBeNull().ShouldContain("Inherit");
    }
}
