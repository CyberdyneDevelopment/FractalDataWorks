using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6939 (slice 3 of FDW-6141): the fence of a run that holds a durable claim. Before each step the run asks whether it still holds its claim; past the lease, or after a renewal found it
/// holds no row, the claim may already be another run's, so the step is refused with the lease code (PIPELINERUNCLAIM-60868) and the run's own step limit is not asked. A run that still
/// holds its claim takes its steps from its own step limit, or freely when it has none.
/// </summary>
public sealed class PipelineRunClaimFenceTests
{
    private sealed class CountingLimit(IGenericResult answer) : IFlowStepLimit
    {
        public int Asked { get; private set; }

        public IGenericResult TakeStep(string stepName)
        {
            Asked++;
            return answer;
        }
    }

    private static IGenericResult Held(ILogger logger) => GenericResult.Success();

    private static IGenericResult Lost(ILogger logger) => GenericResult.Failure(new GenericMessage(MessageSeverity.Error, "the lease has passed", "PIPELINERUNCLAIM-60868"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStepOfARunThatLostItsClaimIsRefusedWithTheLeaseCodeAndTheStepLimitIsNotAsked()
    {
        var inner = new CountingLimit(GenericResult.Success());
        var fence = new PipelineRunClaimFence(inner, Lost, NullLogger.Instance);

        var taken = fence.TakeStep("Load orders");

        taken.IsFailure.ShouldBeTrue();
        taken.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60868");
        inner.Asked.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARunThatStillHoldsItsClaimTakesItsStepsFromItsOwnStepLimit()
    {
        var reached = new CountingLimit(GenericResult.Failure(new GenericMessage(MessageSeverity.Error, "no step left", "PIPELINERUN-60845")));
        var fence = new PipelineRunClaimFence(reached, Held, NullLogger.Instance);

        var taken = fence.TakeStep("Load orders");

        reached.Asked.ShouldBe(1);
        taken.IsFailure.ShouldBeTrue();
        taken.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-60845");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARunThatStillHoldsItsClaimAndHasNoStepLimitTakesItsStepsFreely()
    {
        var fence = new PipelineRunClaimFence(null, Held, NullLogger.Instance);

        fence.TakeStep("Load orders").IsSuccess.ShouldBeTrue();
    }
}
