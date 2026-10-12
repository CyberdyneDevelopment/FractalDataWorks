using System;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Tests;

/// <summary>
/// Tests for <see cref="NotificationRuleScope"/> (FDW-1120): a rule with no scope covers everything, and a rule with a scope
/// covers a notification about any one of the workflow, pipeline or schedule it names.
/// </summary>
public sealed class NotificationRuleScopeTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid C = Guid.NewGuid();

    private static NotificationRuleImplementationConfiguration Rule(Guid? workflow = null, Guid? pipeline = null, Guid? schedule = null) => new()
    {
        Name = "rule",
        WorkflowId = workflow,
        PipelineId = pipeline,
        ScheduleId = schedule
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void ARuleWithNoScopeCoversEverythingEvenANotificationAboutNothing()
    {
        NotificationRuleScope.Covers(Rule(), A, B, C).ShouldBeTrue();
        NotificationRuleScope.Covers(Rule(), null, null, null).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void ARuleScopedToAPipelineCoversThatPipelineAndNoOther()
    {
        NotificationRuleScope.Covers(Rule(pipeline: B), null, B, null).ShouldBeTrue();
        NotificationRuleScope.Covers(Rule(pipeline: B), null, A, null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void ARuleWithSeveralScopesCoversAnyOneOfThem()
    {
        var rule = Rule(workflow: A, schedule: C);

        NotificationRuleScope.Covers(rule, A, B, null).ShouldBeTrue();
        NotificationRuleScope.Covers(rule, null, B, C).ShouldBeTrue();
        NotificationRuleScope.Covers(rule, B, B, B).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public void ANotificationAboutNothingIsNotCoveredByAScopedRule()
    {
        NotificationRuleScope.Covers(Rule(pipeline: B), null, null, null).ShouldBeFalse();
    }
}
