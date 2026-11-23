using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// FDW-6140: each step a run takes is a piece of the stream its person follows: it began, then it ended, and how. A piece that cannot be kept fails the step before it acts, or after it, and never goes missing;
/// a run that keeps no stream takes its steps as before.
/// </summary>
public sealed class FlowRunStreamPiecesTests
{
    private static readonly Guid Person = Guid.Parse("66666666-0000-0000-0000-000000000006");

    private sealed class Step(string name, IGenericResult answer) : IFlowStep
    {
        public int Ran { get; private set; }

        public string Name => name;

        public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
        {
            Ran++;
            return Task.FromResult(answer);
        }
    }

    private sealed class Recorder : IRunStream
    {
        public List<(string Kind, string? Step, string? Text)> Pieces { get; } = [];

        public string? FailsOn { get; set; }

        public Task<IGenericResult> Write(FlowRunIdentity identity, string kind, string? step, string? text, CancellationToken cancellationToken = default)
        {
            if (kind == FailsOn)
            {
                return Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("the store is down")));
            }

            Pieces.Add((kind, step, text));
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        public Task<IGenericResult<IReadOnlyList<RunStreamPiece>>> ReadAfter(Guid tenantId, Guid person, Guid claimRootId, long afterSequence, int? take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("the flow only writes");
    }

    private static FlowStepContext Context(Recorder? stream) =>
        new(new FlowVariables(), new FlowRunTally(new FlowExecutor()), Options(), null, new FlowRunIdentity(Guid.NewGuid(), Guid.NewGuid(), []) { PersonId = Person },
            null, null, null, null, stream);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepThatSucceededIsAStartAndAnEndInOrder()
    {
        var stream = new Recorder();

        var ran = await FlowSequence.Run([new Step("send", GenericResult.Success())], Context(stream), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        stream.Pieces.ShouldBe([(RunStreamKinds.StepStarted, "send", (string?)null), (RunStreamKinds.StepEnded, "send", "Succeeded")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepThatFailedEndsWithTheWordFailedAndItsFailureIsKept()
    {
        var stream = new Recorder();

        var ran = await FlowSequence.Run([new Step("send", GenericResult.Failure(new GenericMessage("refused")))], Context(stream), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldBe("refused");
        stream.Pieces[^1].ShouldBe((RunStreamKinds.StepEnded, "send", "Failed"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStartThatCannotBeKeptFailsTheStepBeforeItActs()
    {
        var stream = new Recorder { FailsOn = RunStreamKinds.StepStarted };
        var step = new Step("send", GenericResult.Success());

        var ran = await FlowSequence.Run([step], Context(stream), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        step.Ran.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnEndThatCannotBeKeptFailsTheStepThatActed()
    {
        var stream = new Recorder { FailsOn = RunStreamKinds.StepEnded };
        var step = new Step("send", GenericResult.Success());

        var ran = await FlowSequence.Run([step], Context(stream), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        step.Ran.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatKeepsNoStreamTakesItsStepsAsBefore()
    {
        var step = new Step("send", GenericResult.Success());

        var ran = await FlowSequence.Run([step], Context(null), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue();
        step.Ran.ShouldBe(1);
    }
}
