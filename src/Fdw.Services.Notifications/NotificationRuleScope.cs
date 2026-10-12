using System;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications;

/// <summary>
/// Decides whether a notification rule's scope covers what a notification is about.
/// </summary>
public static class NotificationRuleScope
{
    /// <summary>
    /// A rule with no scope stated covers everything; a rule with a scope covers a notification about any one of the
    /// workflow, pipeline or schedule it names.
    /// </summary>
    /// <param name="rule">The notification rule.</param>
    /// <param name="workflowId">The workflow the notification is about, or null.</param>
    /// <param name="pipelineId">The pipeline the notification is about, or null.</param>
    /// <param name="scheduleId">The schedule the notification is about, or null.</param>
    /// <returns>True when the rule's scope covers the notification.</returns>
    public static bool Covers(
        INotificationRuleImplementationConfiguration rule,
        Guid? workflowId,
        Guid? pipelineId,
        Guid? scheduleId)
    {
        if (rule.PipelineId is null && rule.WorkflowId is null && rule.ScheduleId is null)
        {
            return true;
        }

        return Names(rule.WorkflowId, workflowId)
            || Names(rule.PipelineId, pipelineId)
            || Names(rule.ScheduleId, scheduleId);
    }

    private static bool Names(Guid? ruleScope, Guid? subject) => ruleScope.HasValue && subject.HasValue && ruleScope.Value == subject.Value;
}
