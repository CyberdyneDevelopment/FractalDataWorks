using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Thresholds;
using Moq;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for reporting the waiting runs of each pipeline to the threshold evaluator (FDW-3975): a pipeline is reported with its count, a pipeline whose waiters have drained is
/// reported once with 0 so a rule that fires on entry can clear, a waiting run with no tenant is not counted, a schedule is reported by the Id read for its name, and an evaluator that
/// throws never stops the next pipeline.
/// </summary>
public sealed class WaitThresholdReporterTests
{
    private sealed class Evaluator(Func<ThresholdSubject, IGenericResult<ThresholdReport>>? answer = null) : IThresholdEvaluator
    {
        public List<(ThresholdSubject Subject, IReadOnlyDictionary<string, object?> Values)> Reports { get; } = [];

        public Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
        {
            Reports.Add((subject, values));
            return Task.FromResult(answer?.Invoke(subject) ?? GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], [])));
        }

        public Task<IGenericResult<ThresholdReport>> Judge(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Reporting waiting runs never judges a held set of thresholds.");

        public IReadOnlyList<ThresholdMatch> Holds(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, string escalation) =>
            throw new NotSupportedException("Reporting waiting runs never asks which rules hold a consequence.");
    }

    private static readonly Guid Tenant = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Load = Guid.Parse("22222222-2222-4222-8222-222222222221");
    private static readonly Guid Export = Guid.Parse("22222222-2222-4222-8222-222222222222");

    private static readonly Guid Nightly = Guid.Parse("33333333-3333-4333-8333-333333333333");

    private static PipelineRunQueueEntry Entry(Guid pipelineId, string name, Guid? tenant, string? schedule = null, Guid? scheduleId = null) =>
        new(new PipelineRunIdentity(Guid.NewGuid(), pipelineId, name, tenant, schedule, scheduleId), DateTimeOffset.UnixEpoch, null, null);

    private static WaitThresholdReporter Reporter(Evaluator evaluator) => new(WaitThresholdKit.Scopes(new Mock<IThresholdSource>().Object, evaluator));

    private static PipelineRunQueueSnapshot Waiting(params PipelineRunQueueEntry[] entries) => new([], entries);

    // The server is reported beside the pipelines for each tenant that has a run waiting; these tests are about the pipelines.
    private static List<(ThresholdSubject Subject, IReadOnlyDictionary<string, object?> Values)> Pipelines(Evaluator evaluator) =>
        [.. evaluator.Reports.Where(report => report.Subject.Kind == "Pipeline")];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EachPipelineIsReportedWithItsOwnWaitingRunsAndItsTenant()
    {
        var evaluator = new Evaluator();

        await Reporter(evaluator).Report(
            Waiting(Entry(Load, "Load", Tenant), Entry(Load, "Load", Tenant), Entry(Load, "Load", Tenant), Entry(Export, "Export", Tenant)),
            TestContext.Current.CancellationToken);

        Pipelines(evaluator).Count.ShouldBe(2);
        var load = Pipelines(evaluator).Single(report => report.Subject.Name == "Load");
        load.Subject.ShouldBe(new ThresholdSubject("Pipeline", "Load", Load, Tenant, null));
        load.Values["waitingRuns"].ShouldBe(3L);
        Pipelines(evaluator).Single(report => report.Subject.Name == "Export").Values["waitingRuns"].ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineWhoseWaitersHaveDrainedIsReportedOnceWithZero()
    {
        var evaluator = new Evaluator();
        var reporter = Reporter(evaluator);

        await reporter.Report(Waiting(Entry(Load, "Load", Tenant), Entry(Load, "Load", Tenant)), TestContext.Current.CancellationToken);
        await reporter.Report(Waiting(), TestContext.Current.CancellationToken);
        await reporter.Report(Waiting(), TestContext.Current.CancellationToken);

        Pipelines(evaluator).Select(report => report.Values["waitingRuns"]).ShouldBe([2L, 0L]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AWaitingRunWithNoTenantIsNotCountedAndNoTenantIsAssumed()
    {
        var evaluator = new Evaluator();

        await Reporter(evaluator).Report(Waiting(Entry(Load, "Load", null), Entry(Export, "Export", Tenant)), TestContext.Current.CancellationToken);

        Pipelines(evaluator).Select(report => report.Subject.Name).ShouldBe(["Export"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnEvaluatorThatThrowsNeverStopsTheNextPipelineFromBeingReported()
    {
        var evaluator = new Evaluator(subject => subject.Name == "Load" ? throw new InvalidOperationException("source unreachable") : GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], [])));

        await Reporter(evaluator).Report(Waiting(Entry(Load, "Load", Tenant), Entry(Export, "Export", Tenant)), TestContext.Current.CancellationToken);

        Pipelines(evaluator).Select(report => report.Subject.Name).ShouldBe(["Load", "Export"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AScheduleIsReportedByTheIdTheRunCarriesWithTheRunsItStartedAndDrainsToZeroOnce()
    {
        var evaluator = new Evaluator();
        var reporter = Reporter(evaluator);

        await reporter.Report(
            Waiting(Entry(Load, "Load", Tenant, "Nightly", Nightly), Entry(Export, "Export", Tenant, "Nightly", Nightly), Entry(Export, "Export", Tenant)), TestContext.Current.CancellationToken);
        await reporter.Report(Waiting(), TestContext.Current.CancellationToken);
        await reporter.Report(Waiting(), TestContext.Current.CancellationToken);

        var schedule = evaluator.Reports.Where(report => report.Subject.Kind == "Schedule").ToList();
        schedule.Select(report => report.Values["waitingRuns"]).ShouldBe([2L, 0L]);
        schedule.ShouldAllBe(report => report.Subject == new ThresholdSubject("Schedule", "Nightly", Nightly, Tenant, null));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatNamesAScheduleButCarriesNoIdIsNotCountedForTheScheduleAndItsPipelineStillIs()
    {
        var evaluator = new Evaluator();

        await Reporter(evaluator).Report(Waiting(Entry(Load, "Load", Tenant, "Nightly")), TestContext.Current.CancellationToken);

        evaluator.Reports.Select(report => report.Subject.Kind).ShouldBe(["Pipeline", "Server"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheServerIsReportedForEachTenantWithEveryWaitingRunAndADrainedTenantSeesTheCountTheServerStillHas()
    {
        var evaluator = new Evaluator();
        var reporter = Reporter(evaluator);
        var other = Guid.Parse("55555555-5555-4555-8555-555555555555");

        await reporter.Report(Waiting(Entry(Load, "Load", Tenant), Entry(Export, "Export", other), Entry(Export, "Export", null)), TestContext.Current.CancellationToken);
        await reporter.Report(Waiting(Entry(Export, "Export", other)), TestContext.Current.CancellationToken);

        var server = evaluator.Reports.Where(report => report.Subject.Kind == "Server").ToList();
        server.Count.ShouldBe(4);
        server.Where(report => report.Subject.TenantId == Tenant).Select(report => report.Values["waitingRuns"]).ShouldBe([3L, 1L]);
        server.Where(report => report.Subject.TenantId == other).Select(report => report.Values["waitingRuns"]).ShouldBe([3L, 1L]);
        server.ShouldAllBe(report => report.Subject.Name == "Server" && report.Subject.SubjectId == report.Subject.TenantId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TwoTenantsWithAScheduleOfTheSameNameAreEachReportedByTheirOwnScheduleId()
    {
        var evaluator = new Evaluator();
        var other = Guid.Parse("55555555-5555-4555-8555-555555555555");
        var otherNightly = Guid.Parse("66666666-6666-4666-8666-666666666666");

        await Reporter(evaluator).Report(
            Waiting(Entry(Load, "Load", Tenant, "Nightly", Nightly), Entry(Export, "Export", other, "Nightly", otherNightly)), TestContext.Current.CancellationToken);

        var schedules = evaluator.Reports.Where(report => report.Subject.Kind == "Schedule").Select(report => report.Subject).ToList();
        schedules.ShouldContain(new ThresholdSubject("Schedule", "Nightly", Nightly, Tenant, null));
        schedules.ShouldContain(new ThresholdSubject("Schedule", "Nightly", otherNightly, other, null));
    }
}
