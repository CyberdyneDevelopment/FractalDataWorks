using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// Stateless validator that enforces the policy elevation rules.
/// A child level can only set a policy to an equal or stricter value than the parent's effective policy.
/// "Stricter" direction is defined per-policy-field.
/// </summary>
/// <remarks>
/// Elevation direction table (stricter is greater):
/// <list type="table">
/// <listheader><term>Column</term><description>Stricter Direction</description></listheader>
/// <item><term>StepFailurePolicy</term><description>HaltStage &gt; ContinueStage</description></item>
/// <item><term>StageFailurePolicy</term><description>HaltProject &gt; ContinueProject</description></item>
/// <item><term>MaxParallelPipelines</term><description>Lower numeric value is stricter</description></item>
/// <item><term>RequireApprovalToRun</term><description>true is stricter</description></item>
/// <item><term>AllowResume</term><description>false is stricter</description></item>
/// <item><term>AllowCrossTenant</term><description>false is stricter</description></item>
/// <item><term>ResiliencyPolicyId</term><description>Not ordered — any override is allowed</description></item>
/// </list>
/// </remarks>
public interface IPolicyElevationValidator
{
    /// <summary>
    /// Validates that the stage's explicit (non-null) policy fields are not less strict
    /// than the parent project's effective policy.
    /// </summary>
    /// <param name="stage">The stage configuration being validated.</param>
    /// <param name="parentEffective">The resolved effective policy of the parent project.</param>
    /// <returns>Success, or Failure with one message per violation.</returns>
    IGenericResult Validate(StageConfiguration stage, ExecutionPolicySnapshot parentEffective);

    /// <summary>
    /// Validates that the step's explicit (non-null) policy fields are not less strict
    /// than the parent stage's effective policy.
    /// </summary>
    /// <param name="step">The step configuration being validated.</param>
    /// <param name="parentEffective">The resolved effective policy of the parent stage.</param>
    /// <returns>Success, or Failure with one message per violation.</returns>
    IGenericResult Validate(StepConfiguration step, ExecutionPolicySnapshot parentEffective);
}
