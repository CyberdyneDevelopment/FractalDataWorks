using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Thresholds.WaitThresholdKit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for which thresholds apply to a run (FDW-3161): the schedule's, else the pipeline's, else the server's; states none is a value that means no
/// alert; and a level that states its thresholds against its mode fails naming the level, with nothing assumed.
/// </summary>
public sealed class WaitThresholdResolverTests
{
    private static readonly WaitThreshold ServerLong = Threshold("Queue long", 50);
    private static readonly WaitThreshold PipelineLong = Threshold("AFC backlog", 20, owner: "Pipeline:P", pipeline: "P");
    private static readonly WaitThreshold ScheduleLong = Threshold("Nightly", 3, owner: "Schedule:S");

    private static IGenericResult<IReadOnlyList<WaitThreshold>> Effective(
        WaitThresholdLayer server,
        WaitThresholdLayer pipeline,
        WaitThresholdLayer? schedule) =>
        WaitThresholdResolver.Effective(server, pipeline, schedule, NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void APipelineAndAScheduleThatInheritTakeTheServers()
    {
        var effective = Effective(Layer("Server", "Own", ServerLong), Layer("Pipeline:P", "Inherit"), Layer("Schedule:S", "Inherit"));

        effective.IsSuccess.ShouldBeTrue(effective.CurrentMessage);
        effective.Value!.ShouldBe([ServerLong]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void APipelineWithThresholdsOfItsOwnReplacesTheServers()
    {
        var effective = Effective(Layer("Server", "Own", ServerLong), Layer("Pipeline:P", "Own", PipelineLong), null);

        effective.Value!.ShouldBe([PipelineLong]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AScheduleWinsOverThePipelineAndThePipelineOverTheServer()
    {
        var effective = Effective(Layer("Server", "Own", ServerLong), Layer("Pipeline:P", "Own", PipelineLong), Layer("Schedule:S", "Own", ScheduleLong));

        effective.Value!.ShouldBe([ScheduleLong]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AScheduleThatStatesNoneMeansNoAlertEvenWhenThePipelineAndServerStateSome()
    {
        var effective = Effective(Layer("Server", "Own", ServerLong), Layer("Pipeline:P", "Own", PipelineLong), Layer("Schedule:S", "None"));

        effective.IsSuccess.ShouldBeTrue(effective.CurrentMessage);
        effective.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void APipelineThatStatesNoneMeansNoAlertForARunWithNoScheduleOrAnInheritingOne()
    {
        var server = Layer("Server", "Own", ServerLong);
        var none = Layer("Pipeline:P", "None");

        Effective(server, none, null).Value!.ShouldBeEmpty();
        Effective(server, none, Layer("Schedule:S", "Inherit")).Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AServerThatStatesNoneIsAValueAndMeansNoAlertForLevelsThatInherit()
    {
        var effective = Effective(Layer("Server", "None"), Layer("Pipeline:P", "Inherit"), null);

        effective.IsSuccess.ShouldBeTrue(effective.CurrentMessage);
        effective.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AServerThatInheritsFailsBecauseItHasNoLevelAboveIt()
    {
        var effective = Effective(Layer("Server", "Inherit"), Layer("Pipeline:P", "Inherit"), null);

        effective.IsFailure.ShouldBeTrue();
        effective.CurrentMessage.ShouldNotBeNull().ShouldContain("Inherit");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALevelThatStatesThresholdsOfItsOwnButListsNoneFailsNamingIt()
    {
        var effective = Effective(Layer("Server", "Own", ServerLong), Layer("Pipeline:AfcPlayersLoad", "Own"), null);

        effective.IsFailure.ShouldBeTrue();
        effective.CurrentMessage.ShouldNotBeNull().ShouldContain("Pipeline:AfcPlayersLoad");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALevelThatStatesNoneButListsThresholdsFailsNamingIt()
    {
        var effective = Effective(Layer("Server", "Own", ServerLong), Layer("Pipeline:P", "None", PipelineLong), null);

        effective.IsFailure.ShouldBeTrue();
        effective.CurrentMessage.ShouldNotBeNull().ShouldContain("Pipeline:P");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALevelThatInheritsButListsThresholdsFailsNamingIt()
    {
        var effective = Effective(Layer("Server", "Own", ServerLong), Layer("Pipeline:P", "Inherit"), Layer("Schedule:S", "Inherit", ScheduleLong));

        effective.IsFailure.ShouldBeTrue();
        effective.CurrentMessage.ShouldNotBeNull().ShouldContain("Schedule:S");
    }
}
