using System;

namespace Fdw.Services.Etl.Projects.Policy;

/// <summary>
/// Options class bound from the <c>ProjectServerDefaults</c> appsettings section.
/// All properties match the 7 nullable policy columns that flow down to Project/Stage/Step.
/// </summary>
/// <remarks>
/// Missing or null values default to the hard-coded fallbacks below.
/// In non-Development environments, all values must be explicitly configured.
/// </remarks>
public sealed class ServerPolicyDefaultsOptions
{
    /// <summary>Section name in appsettings.</summary>
    public const string SectionName = "ProjectServerDefaults";

    /// <summary>
    /// Default step failure policy. Valid values: "HaltStage", "ContinueStage".
    /// Default: "HaltStage" (strict fail-fast).
    /// </summary>
    public string? StepFailurePolicy { get; set; }

    /// <summary>
    /// Default stage failure policy. Valid values: "HaltProject", "ContinueProject".
    /// Default: "HaltProject" (strict fail-fast).
    /// </summary>
    public string? StageFailurePolicy { get; set; }

    /// <summary>
    /// Default maximum number of pipelines that may run in parallel within a Step.
    /// Default: 4.
    /// </summary>
    public int? MaxParallelPipelines { get; set; }

    /// <summary>
    /// Whether explicit approval is required by default. Default: false.
    /// </summary>
    public bool? RequireApprovalToRun { get; set; }

    /// <summary>
    /// Whether failed executions may be resumed by default. Default: false.
    /// </summary>
    public bool? AllowResume { get; set; }

    /// <summary>
    /// Whether cross-tenant pipeline composition is allowed by default. Default: false.
    /// </summary>
    public bool? AllowCrossTenant { get; set; }

    /// <summary>
    /// Server default resiliency policy identifier. Null = no resiliency at server level.
    /// Must be set in non-Development environments (validated at startup).
    /// </summary>
    public Guid? ResiliencyPolicyId { get; set; }
}
