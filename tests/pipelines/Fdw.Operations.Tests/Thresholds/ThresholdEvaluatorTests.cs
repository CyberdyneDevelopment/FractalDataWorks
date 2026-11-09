using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Thresholds;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>
/// Tests for the one threshold evaluator (FDW-3936): the rules of a threshold are judged in order over the reported values with the filter model, every rule that matches fires
/// its consequence, a rule that names an unreported value is not judged, a subject with no threshold is not judged, and a consequence that cannot act says so.
/// </summary>
public sealed class ThresholdEvaluatorTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly ThresholdSubject Pipeline = new("Pipeline", "monthly-sales", Guid.Parse("33333333-3333-3333-3333-333333333333"), Tenant, Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private sealed class RunType : ThresholdTypeBase
    {
        public RunType() : base("TestPipelineRun", "A pipeline run in a test.")
        {
        }

        public override string SubjectKind => "Pipeline";

        public override IReadOnlyList<ThresholdField> Fields { get; } =
        [
            new("consecutiveFailures", typeof(int), "runs", "Runs in a row that failed."),
            new("rowsRejected", typeof(long), "rows", "Rows rejected."),
        ];
    }

    private sealed class Source(params IThresholdDefinition[] thresholds) : IThresholdSource
    {
        public Task<IGenericResult<IReadOnlyList<IThresholdDefinition>>> For(ThresholdSubject subject, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IReadOnlyList<IThresholdDefinition>>>(GenericResult<IReadOnlyList<IThresholdDefinition>>.Success(thresholds));
    }

    private sealed class Services : IServiceProvider
    {
        public Mock<IEscalationAlerts> Alerts { get; } = new();

        public Mock<IRunStopper> Stopper { get; } = new();

        public Mock<IPipelineDisabler> Disabler { get; } = new();

        public object? GetService(Type serviceType) =>
            serviceType == typeof(IEscalationAlerts) ? Alerts.Object
            : serviceType == typeof(IRunStopper) ? Stopper.Object
            : serviceType == typeof(IPipelineDisabler) ? Disabler.Object
            : null;
    }

    private static IFilterNode AtLeast(string field, object value) =>
        new FilterCondition { PropertyName = field, Operator = FilterOperators.ByName("GreaterThanOrEqual"), Value = value };

    private static Threshold<RunType> Failures(bool enabled = true, params ThresholdRule[] rules) => new(new RunType(), "FailureRule", enabled, false, new Dictionary<string, object?>(), rules);

    private static Threshold<RunType> OnEntry(params ThresholdRule[] rules) => new(new RunType(), "FailureRule", true, true, new Dictionary<string, object?>(), rules);

    private static ThresholdRule Alert(IFilterNode criteria) => new(criteria, "Alert", "Ops", []);

    private static ThresholdRule Disable(IFilterNode criteria) => new(criteria, "DisablePipeline", null, []);

    private static IReadOnlyDictionary<string, object?> Values(int failures) => new Dictionary<string, object?> { ["consecutiveFailures"] = failures };

    private static ThresholdEvaluator Sut(Services services, params IThresholdDefinition[] thresholds) => new(new Source(thresholds), services, new ThresholdEntryTracker());

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EveryRuleThatMatchesFiresItsConsequenceInTheOrderTheyAreStated()
    {
        var services = new Services();
        services.Alerts.Setup(a => a.Open(It.IsAny<EscalationAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EscalationOpened>.Success(new EscalationOpened(EscalationOpenOutcomes.Opened, null)));
        services.Disabler.Setup(d => d.Disable(It.IsAny<EscalationFiring>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var threshold = Failures(true, Alert(AtLeast("consecutiveFailures", 3)), Disable(AtLeast("consecutiveFailures", 5)));

        var report = (await Sut(services, threshold).Report(Pipeline, Values(5), TestContext.Current.CancellationToken)).Value!;

        report.ThresholdsJudged.ShouldBe(1);
        report.Fired.Select(fired => (fired.RuleNumber, fired.Escalation)).ToList().ShouldBe([(1, "Alert"), (2, "DisablePipeline")]);
        report.Fired.ShouldAllBe(fired => fired.Result.IsSuccess);
        services.Alerts.Verify(a => a.Open(It.Is<EscalationAlertRequest>(r => r.PolicyName == "Ops" && r.SubjectName == "monthly-sales" && r.ThresholdName == "FailureRule" && r.TenantId == Tenant), It.IsAny<CancellationToken>()), Times.Once);
        services.Disabler.Verify(d => d.Disable(It.Is<EscalationFiring>(f => f.Subject == Pipeline && f.RuleNumber == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARuleThatDoesNotMatchFiresNothing()
    {
        var services = new Services();
        var threshold = Failures(true, Alert(AtLeast("consecutiveFailures", 3)), Disable(AtLeast("consecutiveFailures", 5)));

        var report = (await Sut(services, threshold).Report(Pipeline, Values(2), TestContext.Current.CancellationToken)).Value!;

        report.ThresholdsJudged.ShouldBe(1);
        report.Fired.ShouldBeEmpty();
        services.Alerts.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASubjectWithNoThresholdOrOnlyDisabledOnesIsNotJudged()
    {
        var services = new Services();

        var none = (await Sut(services).Report(Pipeline, Values(9), TestContext.Current.CancellationToken)).Value!;
        var disabled = (await Sut(services, Failures(false, Disable(AtLeast("consecutiveFailures", 1)))).Report(Pipeline, Values(9), TestContext.Current.CancellationToken)).Value!;

        none.ThresholdsJudged.ShouldBe(0);
        none.Fired.ShouldBeEmpty();
        disabled.ThresholdsJudged.ShouldBe(0);
        disabled.Fired.ShouldBeEmpty();
        services.Disabler.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARuleThatNamesAValueTheSubjectDidNotReportIsNotMeasuredAndNeverReadAsZero()
    {
        var services = new Services();
        var threshold = Failures(true, Alert(new FilterCondition { PropertyName = "rowsRejected", Operator = FilterOperators.ByName("LessThan"), Value = 10L }));

        var report = (await Sut(services, threshold).Report(Pipeline, Values(0), TestContext.Current.CancellationToken)).Value!;

        report.Fired.ShouldBeEmpty();
        var missing = report.NotMeasured.ShouldHaveSingleItem();
        missing.RuleNumber.ShouldBe(1);
        missing.Fields.ShouldBe(["rowsRejected"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AConsequenceThatCannotActIsInTheReportAndDoesNotStopTheRulesAfterIt()
    {
        var services = new Services();
        services.Disabler.Setup(d => d.Disable(It.IsAny<EscalationFiring>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var noPolicy = new ThresholdRule(AtLeast("consecutiveFailures", 1), "Alert", null, []);
        var threshold = Failures(true, noPolicy, Disable(AtLeast("consecutiveFailures", 1)));

        var report = (await Sut(services, threshold).Report(Pipeline, Values(1), TestContext.Current.CancellationToken)).Value!;

        report.Fired.Count.ShouldBe(2);
        report.Fired[0].Result.IsFailure.ShouldBeTrue();
        report.Fired[1].Result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AConsequenceTheHostHasNoServiceForSaysSoAndAConsequenceThatActsOnARunNeedsTheRun()
    {
        var services = new Services();
        var stop = new ThresholdRule(AtLeast("consecutiveFailures", 1), "StopRun", null, []);
        var connection = new ThresholdRule(AtLeast("consecutiveFailures", 1), "DisableConnection", null, []);

        var withRun = (await Sut(services, Failures(true, connection)).Report(Pipeline, Values(1), TestContext.Current.CancellationToken)).Value!;
        var withoutRun = (await Sut(services, Failures(true, stop)).Report(Pipeline with { RunId = null }, Values(1), TestContext.Current.CancellationToken)).Value!;

        withRun.Fired.ShouldHaveSingleItem().Result.IsFailure.ShouldBeTrue();
        withoutRun.Fired.ShouldHaveSingleItem().Result.IsFailure.ShouldBeTrue();
        services.Stopper.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARuleThatNamesAConsequenceThatDoesNotExistFailsLoudly()
    {
        var bad = new ThresholdRule(AtLeast("consecutiveFailures", 1), "Shout", null, []);

        var report = (await Sut(new Services(), Failures(true, bad)).Report(Pipeline, Values(1), TestContext.Current.CancellationToken)).Value!;

        report.Fired.ShouldHaveSingleItem().Result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AThresholdThatFiresOnEntryFiresWhenItsCriteriaStartToHoldAndNotAgainUntilTheyStopped()
    {
        var services = new Services();
        services.Disabler.Setup(d => d.Disable(It.IsAny<EscalationFiring>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var sut = Sut(services, OnEntry(Disable(AtLeast("consecutiveFailures", 3))));
        var token = TestContext.Current.CancellationToken;

        var first = (await sut.Report(Pipeline, Values(3), token)).Value!;
        var second = (await sut.Report(Pipeline, Values(4), token)).Value!;
        await sut.Report(Pipeline, Values(0), token);
        var third = (await sut.Report(Pipeline, Values(3), token)).Value!;

        first.Fired.Count.ShouldBe(1);
        second.Fired.ShouldBeEmpty();
        third.Fired.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASuppliedSetIsJudgedInPlaceOfTheAttachedOnes()
    {
        var services = new Services();
        services.Disabler.Setup(d => d.Disable(It.IsAny<EscalationFiring>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var attached = Failures(true, Alert(AtLeast("consecutiveFailures", 1)));
        var supplied = Failures(true, Disable(AtLeast("consecutiveFailures", 1)));

        var report = (await Sut(services, attached).Judge(Pipeline, [supplied], Values(1), TestContext.Current.CancellationToken)).Value!;

        report.Fired.ShouldHaveSingleItem().Escalation.ShouldBe("DisablePipeline");
        services.Alerts.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void HoldsFindsTheRulesOfAConsequenceThatMatchWithoutFiringAnything()
    {
        var services = new Services();
        var hold = new ThresholdRule(AtLeast("consecutiveFailures", 2), "HoldNewRuns", null, []);
        var threshold = Failures(true, Alert(AtLeast("consecutiveFailures", 1)), hold);

        var held = Sut(services, threshold).Holds(Pipeline, [threshold], Values(2), "HoldNewRuns");
        var notHeld = Sut(services, threshold).Holds(Pipeline, [threshold], Values(1), "HoldNewRuns");

        held.ShouldHaveSingleItem().RuleNumber.ShouldBe(2);
        notHeld.ShouldBeEmpty();
        services.Alerts.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheFieldsACriteriaNamesAreListedOnceEachThroughGroups()
    {
        var criteria = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes = [AtLeast("consecutiveFailures", 3), new FilterGroup { Operator = LogicalOperator.And, Nodes = [AtLeast("rowsRejected", 1L), AtLeast("CONSECUTIVEFAILURES", 1)] }],
        };

        ThresholdCriteria.FieldsNamed(criteria).ShouldBe(["consecutiveFailures", "rowsRejected"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheConsequencesAreTheFeatureStatesPlusHoldNewRunsAndAlertNeedsAPolicy()
    {
        EscalationTypes.All().Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal).ToList()
            .ShouldBe(["Alert", "DisableConnection", "DisablePipeline", "HoldNewRuns", "Notify", "StopRun"]);
        EscalationTypes.ByName("Alert").NeedsPolicy.ShouldBeTrue();
        EscalationTypes.ByName("Notify").NeedsNotificationRules.ShouldBeTrue();
        EscalationTypes.ByName("StopRun").ActsOnRun.ShouldBeTrue();
    }
}
