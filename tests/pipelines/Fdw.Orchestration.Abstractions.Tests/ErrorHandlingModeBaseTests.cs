using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;
using Fdw.Results;
using Fdw.Messages;

namespace Fdw.Orchestration.Abstractions.Tests;

public class ErrorHandlingModeBaseTests
{
    private sealed class StopOnErrorMode : ErrorHandlingModeBase
    {
        public StopOnErrorMode()
            : base("StopOnError",
                continuesExecution: false,
                supportsRetry: false,
                triggersCompensation: false)
        {
        }

        public override Task<IGenericResult> HandleError(
            Exception error,
            IOrchestrationStepExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("Execution stopped")));
        }
    }

    private sealed class RetryOnErrorMode : ErrorHandlingModeBase
    {
        public RetryOnErrorMode()
            : base("RetryOnError",
                continuesExecution: true,
                supportsRetry: true,
                triggersCompensation: false)
        {
        }

        public override Task<IGenericResult> HandleError(
            Exception error,
            IOrchestrationStepExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    private sealed class CompensateOnErrorMode : ErrorHandlingModeBase
    {
        public CompensateOnErrorMode()
            : base("CompensateOnError",
                continuesExecution: false,
                supportsRetry: false,
                triggersCompensation: true)
        {
        }

        public override Task<IGenericResult> HandleError(
            Exception error,
            IOrchestrationStepExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StopOnErrorSetsPropertiesCorrectly()
    {
        var mode = new StopOnErrorMode();
        mode.Name.ShouldBe("StopOnError");
        mode.ContinuesExecution.ShouldBeFalse();
        mode.SupportsRetry.ShouldBeFalse();
        mode.TriggersCompensation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RetryOnErrorSetsPropertiesCorrectly()
    {
        var mode = new RetryOnErrorMode();

        mode.ContinuesExecution.ShouldBeTrue();
        mode.SupportsRetry.ShouldBeTrue();
        mode.TriggersCompensation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CompensateOnErrorSetsPropertiesCorrectly()
    {
        var mode = new CompensateOnErrorMode();

        mode.ContinuesExecution.ShouldBeFalse();
        mode.SupportsRetry.ShouldBeFalse();
        mode.TriggersCompensation.ShouldBeTrue();
    }
}
