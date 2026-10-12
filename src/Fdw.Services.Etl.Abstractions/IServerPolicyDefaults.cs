using System;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Server-level baseline policy values, bound from the <c>ProjectServerDefaults</c> appsettings section.
/// These values serve as the root of the inheritance chain when a Project, Stage, or Step
/// has NULL for a policy column (NULL = inherit from parent effective value).
/// </summary>
/// <remarks>
/// The 7 policy columns at each level default to these server values when NULL.
/// Child levels can only make policies equal to or stricter than their parent's effective value.
/// </remarks>
public interface IServerPolicyDefaults
{
    /// <summary>
    /// Gets the default behavior when a Pipeline within a Step fails.
    /// Corresponds to <c>StepFailurePolicy</c> column. Default: "HaltStage".
    /// </summary>
    string StepFailurePolicy { get; }

    /// <summary>
    /// Gets the default behavior when a Stage within a Project fails.
    /// Corresponds to <c>StageFailurePolicy</c> column. Default: "HaltProject".
    /// </summary>
    string StageFailurePolicy { get; }

    /// <summary>
    /// Gets the default maximum number of pipelines that may run in parallel within a Step.
    /// Corresponds to <c>MaxParallelPipelines</c> column. Default: server-configured value.
    /// </summary>
    int MaxParallelPipelines { get; }

    /// <summary>
    /// Gets whether execution requires explicit approval by default.
    /// Corresponds to <c>RequireApprovalToRun</c> column. Default: false.
    /// </summary>
    bool RequireApprovalToRun { get; }

    /// <summary>
    /// Gets whether failed executions can be resumed by default.
    /// Corresponds to <c>AllowResume</c> column. Default: false.
    /// </summary>
    bool AllowResume { get; }

    /// <summary>
    /// Gets whether cross-tenant pipeline composition is allowed by default.
    /// Corresponds to <c>AllowCrossTenant</c> column. Default: false.
    /// </summary>
    bool AllowCrossTenant { get; }

    /// <summary>
    /// Gets the server default resiliency policy identifier, or null if no default is configured.
    /// Corresponds to <c>ResiliencyPolicyId</c> column. Must be set in non-Development environments.
    /// </summary>
    Guid? ResiliencyPolicyId { get; }
}
