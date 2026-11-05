using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// What the ETL host reports to the threshold evaluator after a run (FDW-3864): the failures in a row that count, from the pipeline's last change, with the newest category and the
/// counted failures; nothing for a pipeline that is disabled; and the consequence of a rule that fires is the evaluator's, so a failed consequence fails the report.
/// </summary>
public sealed class PipelineFailureReporterTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Run = new("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Changed = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private sealed class Configurations(GraphPipelineConfiguration? graph, bool disableSucceeds = true) : IPipelineFailureConfigurations
    {
        public int Disabled { get; private set; }

        public Task<IGenericResult<GraphPipelineConfiguration?>> Read(string pipelineName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<GraphPipelineConfiguration?>>(GenericResult<GraphPipelineConfiguration?>.Success(graph));

        public Task<IGenericResult> Disable(GraphPipelineConfiguration pipeline, CancellationToken cancellationToken = default)
        {
            Disabled++;
            pipeline.IsEnabled = false;
            return Task.FromResult(disableSucceeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the save was refused")));
        }
    }

    private sealed class History(params PipelineRunOutcome[] newestFirst) : IPipelineRunHistory
    {
        public Task<IGenericResult<IReadOnlyList<PipelineRunOutcome>>> Page(string pipelineName, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IReadOnlyList<PipelineRunOutcome>>>(GenericResult<IReadOnlyList<PipelineRunOutcome>>.Success([.. newestFirst.Skip(skip).Take(take)]));
    }

    private static readonly Guid PipelineId = new("cccccccc-0000-0000-0000-000000000001");

    private static GraphPipelineConfiguration Graph(bool enabled = true) => new() { Id = PipelineId, Name = "Orders", IsEnabled = enabled, ModifyDate = Changed };

    private static PipelineRunOutcome Failed(int minutes, string? category = "Validation", string? code = "FDW-20000") =>
        new(Guid.NewGuid(), Changed.AddMinutes(minutes), false, true, category, code);

    private static PipelineRunOutcome Succeeded(int minutes) => new(Guid.NewGuid(), Changed.AddMinutes(minutes), true, false, null, null);

    private static (Mock<IThresholdEvaluator> Evaluator, List<(ThresholdSubject Subject, IReadOnlyDictionary<string, object?> Values)> Seen) Capturing()
    {
        var seen = new List<(ThresholdSubject, IReadOnlyDictionary<string, object?>)>();
        var evaluator = new Mock<IThresholdEvaluator>();
        evaluator.Setup(e => e.Report(It.IsAny<ThresholdSubject>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Callback((ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken _) => seen.Add((subject, values)))
            .ReturnsAsync(GenericResult<ThresholdReport>.Success(new ThresholdReport(1, [], [])));
        return (evaluator, seen);
    }

    private static Mock<IThresholdEvaluator> Evaluator(IGenericResult? consequence = null)
    {
        var fired = consequence is null ? [] : new List<ThresholdFired> { new("Runs", 1, "DisablePipeline", "consecutiveFailures >= 3", consequence) };
        var evaluator = new Mock<IThresholdEvaluator>();
        evaluator.Setup(e => e.Report(It.IsAny<ThresholdSubject>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ThresholdReport>.Success(new ThresholdReport(1, fired, [])));
        return evaluator;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheCountTheNewestCategoryAndTheCountedFailuresAreReportedForThePipelineAndTheRun()
    {
        var (evaluator, seen) = Capturing();
        var reporter = new PipelineFailureReporter(new Configurations(Graph()), new History(Failed(30, "Missing"), Failed(20), Succeeded(10), Failed(5)), evaluator.Object);

        var result = await reporter.Report("Orders", Run, Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var (subject, values) = seen.ShouldHaveSingleItem();
        subject.ShouldBe(new ThresholdSubject("Pipeline", "Orders", PipelineId, Tenant, Run));
        values[PipelineRunThresholdType.ConsecutiveFailures].ShouldBe(2);
        values[PipelineRunThresholdType.LastFailureCategory].ShouldBe("Missing");
        var carried = values[PipelineRunThresholdType.Failures].ShouldBeOfType<string>();
        JsonSerializer.Deserialize<List<PipelineRunOutcome>>(carried, (JsonSerializerOptions?)null).ShouldNotBeNull().Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailureThatCarriesNoCategoryReportsNoCategoryNotAStandInWord()
    {
        var (evaluator, seen) = Capturing();
        var reporter = new PipelineFailureReporter(new Configurations(Graph()), new History(Failed(30, null, null)), evaluator.Object);

        await reporter.Report("Orders", Run, Tenant, TestContext.Current.CancellationToken);

        var (_, values) = seen.ShouldHaveSingleItem();
        values.ContainsKey(PipelineRunThresholdType.LastFailureCategory).ShouldBeFalse();
        values[PipelineRunThresholdType.ConsecutiveFailures].ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatIsAlreadyDisabledIsNotReported()
    {
        var evaluator = Evaluator();
        var reporter = new PipelineFailureReporter(new Configurations(Graph(enabled: false)), new History(Failed(30)), evaluator.Object);

        var result = await reporter.Report("Orders", Run, Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        evaluator.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunWithNoTenantIsNotReportedAndNothingIsAssumed()
    {
        var evaluator = Evaluator();
        var reporter = new PipelineFailureReporter(new Configurations(Graph()), new History(Failed(30)), evaluator.Object);

        var result = await reporter.Report("Orders", Run, null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        evaluator.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AConsequenceThatFailedFailsTheReport()
    {
        var reporter = new PipelineFailureReporter(
            new Configurations(Graph()), new History(Failed(30)), Evaluator(GenericResult.Failure(new GenericMessage("the policy does not exist"))).Object);

        var result = await reporter.Report("Orders", Run, Tenant, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ThresholdsThatCouldNotBeJudgedFailTheReport()
    {
        var evaluator = new Mock<IThresholdEvaluator>();
        evaluator.Setup(e => e.Report(It.IsAny<ThresholdSubject>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ThresholdReport>.Failure(new GenericMessage("the thresholds could not be read")));
        var reporter = new PipelineFailureReporter(new Configurations(Graph()), new History(Failed(30)), evaluator.Object);

        (await reporter.Report("Orders", Run, Tenant, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }
}
