using System;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// A pipeline an Execute pipeline component starts carries the run's person (FDW-6137), as it carries the run's tenant and roles; a run with no person
/// starts a pipeline with none.
/// </summary>
public sealed class FlowExecutePipelinePersonTests
{
    private static readonly Guid Person = Guid.Parse("66666666-0000-0000-0000-000000000006");

    private static FlowExecutePipeline Step() =>
        new(
            "tidy",
            "Cleanup",
            [],
            awaitCompletion: false,
            resultVariable: null,
            failOnChildFailure: false,
            logger: null,
            runPoint: RunPoints.ByName("AfterLoad"),
            run: new FlowNotifyRun(Guid.NewGuid(), "Nightly", Guid.NewGuid(), DateTimeOffset.UtcNow),
            loaded: () => 0);

    private static FlowStepContext Context(FakeRunStarter starter, Guid? person) =>
        new(new FlowVariables(), new FlowRunTally(new FlowExecutor()), Options(), starter,
            new FlowRunIdentity(Guid.NewGuid(), Guid.NewGuid(), ["etl-runner"]) { PersonId = person });

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AStartedPipelineCarriesTheRunsPersonOrNoneWhenTheRunHasNone(bool hasPerson)
    {
        var starter = new FakeRunStarter();
        Guid? person = hasPerson ? Person : null;

        var reached = await Step().Run(Context(starter, person), TestContext.Current.CancellationToken);

        reached.IsSuccess.ShouldBeTrue(reached.CurrentMessage);
        starter.Started.ShouldHaveSingleItem().PersonId.ShouldBe(person);
    }
}
