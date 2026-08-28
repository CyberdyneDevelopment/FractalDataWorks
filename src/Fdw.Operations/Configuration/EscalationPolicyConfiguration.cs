using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>
/// Configuration for escalation policies.
/// Defines when and how to notify stakeholders about execution issues.
/// </summary>
/// <remarks>
/// <para>
/// Escalation policies enable automated response to execution failures and anomalies:
/// <list type="bullet">
///   <item><description><strong>Multi-level escalation</strong> - Progressive notifications based on severity and duration</description></item>
///   <item><description><strong>Scoped applicability</strong> - Apply to specific ItemTypes, Workflows, or Schedules</description></item>
///   <item><description><strong>Conditional triggering</strong> - Optional expressions for advanced logic</description></item>
///   <item><description><strong>Cooldown periods</strong> - Prevent notification spam</description></item>
/// </list>
/// </para>
/// <para>
/// Each policy contains:
/// <list type="bullet">
///   <item><description>Scope definition (what triggers the policy)</description></item>
///   <item><description>Maximum escalation level (how far to escalate)</description></item>
///   <item><description>Cooldown period (how often to escalate)</description></item>
///   <item><description>Child EscalationLevels (who to notify and when)</description></item>
/// </list>
/// </para>
/// <para>
/// Example workflow:
/// <list type="number">
///   <item><description>Job fails after max retries</description></item>
///   <item><description>Policy matches based on WorkflowId</description></item>
///   <item><description>Level 1: Immediate email to team</description></item>
///   <item><description>If unresolved after 15 min → Level 2: Teams channel notification</description></item>
///   <item><description>If unresolved after 60 min → Level 3: PagerDuty alert to on-call</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Operations")]
public sealed partial class EscalationPolicyConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Operationss";

    /// <inheritdoc />
    public string ServiceType => "Operations";

    /// <inheritdoc />
    public string? ServiceOptionType => null;


    /// <summary>
    /// Gets or sets the unique identifier for this escalation policy.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this escalation policy.
    /// </summary>
    /// <remarks>
    /// Should be descriptive and unique. Examples:
    /// - "Critical Workflow Failures"
    /// - "NFL Data Import Escalation"
    /// - "Nightly ETL Pipeline Alerts"
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of the policy's purpose and scope.
    /// </summary>
    /// <remarks>
    /// Useful for documentation and operator understanding.
    /// Should explain when this policy applies and why.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this policy is currently active.
    /// </summary>
    /// <remarks>
    /// Disabled policies are not evaluated during execution monitoring.
    /// Use for temporary suspension without deletion (e.g., during maintenance windows).
    /// </remarks>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the item type this policy applies to.
    /// Null means applies to all item types.
    /// </summary>
    /// <value>
    /// Examples: "Workflow", "Job", "Stage", "Step".
    /// </value>
    /// <remarks>
    /// Filters which ExecutionItems trigger this policy.
    /// Can be combined with WorkflowId or ScheduleId for narrower scope.
    /// </remarks>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets the specific workflow ID this policy applies to.
    /// Null means applies to all workflows.
    /// </summary>
    /// <remarks>
    /// Narrows scope to a single workflow definition.
    /// Useful for workflow-specific escalation paths.
    /// </remarks>
    public Guid? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the specific schedule ID this policy applies to.
    /// Null means applies to all schedules.
    /// </summary>
    /// <remarks>
    /// Narrows scope to jobs triggered by a specific schedule.
    /// Useful for scheduled job failures requiring immediate attention.
    /// </remarks>
    public Guid? ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the maximum escalation level for this policy.
    /// </summary>
    /// <remarks>
    /// Escalation stops after reaching this level.
    /// Typical values: 2-4 levels.
    /// Level 1 = initial notification, higher levels = more urgent channels/recipients.
    /// </remarks>
    public int MaxEscalationLevel { get; set; } = 3;

    /// <summary>
    /// Gets or sets the cooldown period in minutes between escalation cycles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prevents repeated escalation notifications for the same issue.
    /// </para>
    /// <para>
    /// Example: If set to 60 minutes and an issue persists:
    /// - Initial escalation at T+0
    /// - No further escalation until T+60 (cooldown expires)
    /// - Escalation cycle restarts if issue still unresolved
    /// </para>
    /// Typical values: 30-120 minutes.
    /// </remarks>
    public int CooldownMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets an optional condition expression for policy triggering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Advanced feature for conditional escalation based on execution context.
    /// </para>
    /// <para>
    /// Examples (syntax TBD based on expression evaluator):
    /// <list type="bullet">
    ///   <item><description>"DurationMs &gt; 300000" - Only if execution exceeds 5 minutes</description></item>
    ///   <item><description>"State == 'Failed' AND RetryCount &gt; 3" - Multiple failures</description></item>
    ///   <item><description>"TriggerSource.StartsWith('Schedule:')" - Only scheduled jobs</description></item>
    /// </list>
    /// </para>
    /// Null means always evaluate escalation levels.
    /// </remarks>
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before escalation.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the delay in seconds between retry attempts.
    /// </summary>
    public int RetryDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets the collection of escalation levels for this policy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each level defines when and how to notify stakeholders as an issue escalates.
    /// Levels are ordered by their Level property (1, 2, 3, etc.).
    /// </para>
    /// Populated automatically from workflow.EscalationLevel child rows during configuration loading.
    /// </remarks>
    public IList<EscalationLevelConfiguration> Levels { get; set; } = [];
}
