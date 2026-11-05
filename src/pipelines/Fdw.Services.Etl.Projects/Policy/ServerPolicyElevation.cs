using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Policy;

/// <summary>
/// Which policy settings would be less strict than the server's defaults: a project or node may tighten a policy, never
/// relax it below the server's.
/// </summary>
/// <remarks>
/// Why one place: the project and node validators held the same six comparisons word for word against the defaults they
/// were constructed with. They now read the defaults when they validate (FDW-3580), and both ask here.
/// </remarks>
internal static class ServerPolicyElevation
{
    /// <summary>The messages for each setting that relaxes the server's default; empty when none does.</summary>
    public static IReadOnlyList<string> Violations(
        string? stepFailurePolicy,
        string? stageFailurePolicy,
        int? maxParallelPipelines,
        bool? requireApprovalToRun,
        bool? allowResume,
        bool? allowCrossTenant,
        IServerPolicyDefaultImplementationConfiguration defaults)
    {
        var violations = new List<string>();

        if (string.Equals(stepFailurePolicy, "ContinueStage", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(defaults.StepFailurePolicy, "HaltStage", StringComparison.OrdinalIgnoreCase))
            violations.Add("StepFailurePolicy 'ContinueStage' is less strict than server default 'HaltStage'.");

        if (string.Equals(stageFailurePolicy, "ContinueProject", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(defaults.StageFailurePolicy, "HaltProject", StringComparison.OrdinalIgnoreCase))
            violations.Add("StageFailurePolicy 'ContinueProject' is less strict than server default 'HaltProject'.");

        if (maxParallelPipelines is { } parallel && parallel > defaults.MaxParallelPipelines)
            violations.Add($"MaxParallelPipelines {parallel} exceeds server default {defaults.MaxParallelPipelines}.");

        if (requireApprovalToRun is false && defaults.RequireApprovalToRun)
            violations.Add("RequireApprovalToRun cannot be false when server default is true.");

        if (allowResume is true && !defaults.AllowResume)
            violations.Add("AllowResume cannot be true when server default is false.");

        if (allowCrossTenant is true && !defaults.AllowCrossTenant)
            violations.Add("AllowCrossTenant cannot be true when server default is false.");

        return violations;
    }
}
