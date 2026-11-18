using Fdw.Orchestration.TypeCollections.BackoffStrategies;
using Fdw.Orchestration.TypeCollections.ErrorHandlingModes;
using Fdw.Orchestration.TypeCollections.ExecutionStatuses;
using BackoffStrategiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions.BackoffStrategies;
using ErrorHandlingModesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions.ErrorHandlingModes;
using ExecutionStatusesCollection = Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions.ExecutionStatuses;

namespace Fdw.Orchestration.Pipelines.Tests;

/// <summary>
/// FDW-6325: the orchestration executor looks these options up by name. They are declared in
/// Fdw.Orchestration.Pipelines and join collections declared in Fdw.Orchestration.Abstractions, so they reach
/// their collections only through the host's module initializer. This project is host-shaped (an executable
/// with the registration generator), so each lookup must answer the declared option and never the NotFound
/// sentinel. Each test compares with NotFound itself, never one lookup with another, which passes sentinel
/// against sentinel.
/// </summary>
public sealed class OrchestrationTypeOptionRegistrationTests
{
    [Fact]
    public void StopOnErrorTheExecutorsDefaultErrorHandlingResolvesByName()
    {
        var mode = ErrorHandlingModesCollection.ByName("StopOnError");

        ReferenceEquals(mode, ErrorHandlingModesCollection.NotFound).ShouldBeFalse();
        mode.Name.ShouldBe("StopOnError");
        mode.ShouldBeOfType<StopOnErrorMode>();
    }

    [Fact]
    public void ExponentialTheExecutorsRetryBackoffResolvesByName()
    {
        var strategy = BackoffStrategiesCollection.ByName("Exponential");

        ReferenceEquals(strategy, BackoffStrategiesCollection.NotFound).ShouldBeFalse();
        strategy.Name.ShouldBe("Exponential");
        strategy.ShouldBeOfType<ExponentialBackoffStrategy>();
    }

    [Theory]
    [InlineData("Succeeded", typeof(SucceededStatus))]
    [InlineData("SucceededWithWarnings", typeof(SucceededWithWarningsStatus))]
    [InlineData("Failed", typeof(FailedStatus))]
    [InlineData("Cancelled", typeof(CancelledStatus))]
    [InlineData("TimedOut", typeof(TimedOutStatus))]
    [InlineData("Running", typeof(RunningStatus))]
    public void EveryExecutionStatusTheExecutorReadsResolvesByName(string name, Type expected)
    {
        var status = ExecutionStatusesCollection.ByName(name);

        ReferenceEquals(status, ExecutionStatusesCollection.NotFound).ShouldBeFalse();
        status.Name.ShouldBe(name);
        status.ShouldBeOfType(expected);
    }

    [Fact]
    public void EveryDeclaredErrorHandlingModeJoinsItsCollection()
    {
        ErrorHandlingModesCollection.All().Select(mode => mode.Name)
            .ShouldBe(["Compensate", "RedirectToDeadLetter", "RetryWithBackoff", "SkipAndContinue", "StopOnError"], ignoreOrder: true);
    }

    [Fact]
    public void EveryDeclaredBackoffStrategyJoinsItsCollection()
    {
        BackoffStrategiesCollection.All().Select(strategy => strategy.Name)
            .ShouldBe(["DecorrelatedJitter", "Exponential", "Fixed", "Linear"], ignoreOrder: true);
    }

    [Fact]
    public void EveryDeclaredExecutionStatusJoinsItsCollection()
    {
        ExecutionStatusesCollection.All().Select(status => status.Name)
            .ShouldBe(["Cancelled", "Failed", "Paused", "Queued", "Running", "Succeeded", "SucceededWithWarnings", "TimedOut"], ignoreOrder: true);
    }
}
