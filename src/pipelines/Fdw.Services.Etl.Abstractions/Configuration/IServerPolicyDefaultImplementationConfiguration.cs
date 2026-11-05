using System;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// The server's policy defaults: the values a project, stage or step setting left unset inherits from the top of the
/// policy cascade. One current row of the ServerPolicyDefault domain names it.
/// </summary>
/// <remarks>
/// Why a configuration row: Mike ruled (2026-11-04) that the cascade may fall back to a server default only when that
/// default is not hard-coded, so the values live in ConfigurationDb with the platform's other settings (FDW-3580).
/// </remarks>
public interface IServerPolicyDefaultImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets the behavior when a pipeline within a step fails, such as "HaltStage".</summary>
    string StepFailurePolicy { get; }

    /// <summary>Gets the behavior when a stage within a project fails, such as "HaltProject".</summary>
    string StageFailurePolicy { get; }

    /// <summary>Gets the maximum number of pipelines that may run in parallel within a step.</summary>
    int MaxParallelPipelines { get; }

    /// <summary>Gets whether a run needs explicit approval.</summary>
    bool RequireApprovalToRun { get; }

    /// <summary>Gets whether a failed run may be resumed.</summary>
    bool AllowResume { get; }

    /// <summary>Gets whether cross-tenant pipeline composition is allowed.</summary>
    bool AllowCrossTenant { get; }

    /// <summary>Gets the resiliency policy, or <see langword="null"/> when the server applies none.</summary>
    Guid? ResiliencyPolicyId { get; }
}
