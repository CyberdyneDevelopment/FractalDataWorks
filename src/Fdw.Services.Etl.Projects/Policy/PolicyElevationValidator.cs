using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Policy;

/// <summary>
/// Stateless implementation of <see cref="IPolicyElevationValidator"/>.
/// Enforces the rule that child policy fields can only be equal to or stricter than the parent effective policy.
/// </summary>
/// <remarks>
/// Elevation direction table (stricter is "greater"):
/// <list type="table">
/// <item><term>StepFailurePolicy</term><description>HaltStage > ContinueStage</description></item>
/// <item><term>StageFailurePolicy</term><description>HaltProject > ContinueProject</description></item>
/// <item><term>MaxParallelPipelines</term><description>Lower numeric value is stricter</description></item>
/// <item><term>RequireApprovalToRun</term><description>true is stricter</description></item>
/// <item><term>AllowResume</term><description>false is stricter</description></item>
/// <item><term>AllowCrossTenant</term><description>false is stricter</description></item>
/// <item><term>ResiliencyPolicyId</term><description>Not ordered — any override allowed</description></item>
/// </list>
/// </remarks>
public sealed class PolicyElevationValidator : IPolicyElevationValidator
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyElevationValidator"/> class.
    /// </summary>
    public PolicyElevationValidator(ILogger<PolicyElevationValidator>? logger = null)
    {
        _logger = logger ?? NullLogger<PolicyElevationValidator>.Instance;
    }

    /// <inheritdoc/>
    public IGenericResult Validate(StageConfiguration stage, ExecutionPolicySnapshot parentEffective)
    {
        if (stage == null) throw new ArgumentNullException(nameof(stage));
        if (parentEffective == null) throw new ArgumentNullException(nameof(parentEffective));

        var violations = CollectViolations(
            stage.StepFailurePolicy,
            stage.StageFailurePolicy,
            stage.MaxParallelPipelines,
            stage.RequireApprovalToRun,
            stage.AllowResume,
            stage.AllowCrossTenant,
            parentEffective);

        if (violations.Count == 0)
            return GenericResult.Success();

        var message = string.Join("; ", violations);
        return GenericResult.Failure(
            ProjectConfigurationLog.PolicyElevationFailed(_logger, "Stage", stage.Name, message));
    }

    /// <inheritdoc/>
    public IGenericResult Validate(StepConfiguration step, ExecutionPolicySnapshot parentEffective)
    {
        if (step == null) throw new ArgumentNullException(nameof(step));
        if (parentEffective == null) throw new ArgumentNullException(nameof(parentEffective));

        var violations = CollectViolations(
            step.StepFailurePolicy,
            step.StageFailurePolicy,
            step.MaxParallelPipelines,
            step.RequireApprovalToRun,
            step.AllowResume,
            step.AllowCrossTenant,
            parentEffective);

        if (violations.Count == 0)
            return GenericResult.Success();

        var message = string.Join("; ", violations);
        return GenericResult.Failure(
            ProjectConfigurationLog.PolicyElevationFailed(_logger, "Step", step.Name, message));
    }

    // Why: Shared violation collection for Stage and Step — both have identical policy field semantics.
    // Split into per-field helpers to stay under complexity threshold.
    private static List<string> CollectViolations(
        string? stepFailurePolicy,
        string? stageFailurePolicy,
        int? maxParallelPipelines,
        bool? requireApprovalToRun,
        bool? allowResume,
        bool? allowCrossTenant,
        ExecutionPolicySnapshot parentEffective)
    {
        var violations = new List<string>(6);

        CheckStepFailurePolicy(stepFailurePolicy, parentEffective, violations);
        CheckStageFailurePolicy(stageFailurePolicy, parentEffective, violations);
        CheckMaxParallelPipelines(maxParallelPipelines, parentEffective, violations);
        CheckRequireApprovalToRun(requireApprovalToRun, parentEffective, violations);
        CheckAllowResume(allowResume, parentEffective, violations);
        CheckAllowCrossTenant(allowCrossTenant, parentEffective, violations);

        return violations;
    }

    private static void CheckStepFailurePolicy(
        string? childValue, ExecutionPolicySnapshot parent, List<string> violations)
    {
        if (childValue is null) return;
        if (string.Equals(childValue, "ContinueStage", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parent.StepFailurePolicy, "HaltStage", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add("StepFailurePolicy: 'ContinueStage' is less strict than parent effective 'HaltStage'");
        }
    }

    private static void CheckStageFailurePolicy(
        string? childValue, ExecutionPolicySnapshot parent, List<string> violations)
    {
        if (childValue is null) return;
        if (string.Equals(childValue, "ContinueProject", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parent.StageFailurePolicy, "HaltProject", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add("StageFailurePolicy: 'ContinueProject' is less strict than parent effective 'HaltProject'");
        }
    }

    private static void CheckMaxParallelPipelines(
        int? childValue, ExecutionPolicySnapshot parent, List<string> violations)
    {
        if (childValue is null) return;
        if (childValue.Value > parent.MaxParallelPipelines)
        {
            violations.Add(
                $"MaxParallelPipelines: {childValue.Value} exceeds parent effective {parent.MaxParallelPipelines} (lower is stricter)");
        }
    }

    private static void CheckRequireApprovalToRun(
        bool? childValue, ExecutionPolicySnapshot parent, List<string> violations)
    {
        if (childValue is null) return;
        if (!childValue.Value && parent.RequireApprovalToRun)
        {
            violations.Add("RequireApprovalToRun: false is less strict than parent effective true");
        }
    }

    private static void CheckAllowResume(
        bool? childValue, ExecutionPolicySnapshot parent, List<string> violations)
    {
        if (childValue is null) return;
        if (childValue.Value && !parent.AllowResume)
        {
            violations.Add("AllowResume: true is less strict than parent effective false");
        }
    }

    private static void CheckAllowCrossTenant(
        bool? childValue, ExecutionPolicySnapshot parent, List<string> violations)
    {
        if (childValue is null) return;
        if (childValue.Value && !parent.AllowCrossTenant)
        {
            violations.Add("AllowCrossTenant: true is less strict than parent effective false");
        }
    }
}
