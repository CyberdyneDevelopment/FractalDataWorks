using FluentValidation;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>
/// Reusable FluentValidation validator that applies policy elevation rules from a parent
/// effective snapshot. Used as a shared validation component by StageConfigurationValidator
/// and StepConfigurationValidator.
/// </summary>
/// <typeparam name="T">The configuration type being validated (Stage or Step).</typeparam>
public abstract class ExecutionPolicyElevationValidator<T> : AbstractValidator<T>
    where T : class
{
    /// <summary>
    /// Registers the elevation rules using the concrete accessor lambdas.
    /// </summary>
    protected void AddElevationRules(
        ExecutionPolicySnapshot parentEffective,
        System.Func<T, string?> stepFailurePolicyGetter,
        System.Func<T, string?> stageFailurePolicyGetter,
        System.Func<T, int?> maxParallelPipelinesGetter,
        System.Func<T, bool?> requireApprovalGetter,
        System.Func<T, bool?> allowResumeGetter,
        System.Func<T, bool?> allowCrossTenantGetter)
    {
        // StepFailurePolicy: HaltStage > ContinueStage
        RuleFor(x => stepFailurePolicyGetter(x))
            .Must((_, val) => IsStepFailurePolicyValid(val, parentEffective.StepFailurePolicy))
            .When(x => stepFailurePolicyGetter(x) is not null)
            .WithMessage(x =>
                $"StepFailurePolicy '{stepFailurePolicyGetter(x)}' is less strict than parent effective '{parentEffective.StepFailurePolicy}'");

        // StageFailurePolicy: HaltProject > ContinueProject
        RuleFor(x => stageFailurePolicyGetter(x))
            .Must((_, val) => IsStageFailurePolicyValid(val, parentEffective.StageFailurePolicy))
            .When(x => stageFailurePolicyGetter(x) is not null)
            .WithMessage(x =>
                $"StageFailurePolicy '{stageFailurePolicyGetter(x)}' is less strict than parent effective '{parentEffective.StageFailurePolicy}'");

        // MaxParallelPipelines: lower is stricter
        RuleFor(x => maxParallelPipelinesGetter(x))
            .Must((_, val) => val is null || val.Value <= parentEffective.MaxParallelPipelines)
            .When(x => maxParallelPipelinesGetter(x) is not null)
            .WithMessage(x =>
                $"MaxParallelPipelines {maxParallelPipelinesGetter(x)} exceeds parent effective {parentEffective.MaxParallelPipelines}");

        // RequireApprovalToRun: true is stricter — cannot relax
        RuleFor(x => requireApprovalGetter(x))
            .Must((_, val) => val is null || val.Value || !parentEffective.RequireApprovalToRun)
            .When(x => requireApprovalGetter(x) is not null)
            .WithMessage("RequireApprovalToRun cannot be set to false when parent effective is true");

        // AllowResume: false is stricter — cannot relax
        RuleFor(x => allowResumeGetter(x))
            .Must((_, val) => val is null || !val.Value || parentEffective.AllowResume)
            .When(x => allowResumeGetter(x) is not null)
            .WithMessage("AllowResume cannot be set to true when parent effective is false");

        // AllowCrossTenant: false is stricter — cannot relax
        RuleFor(x => allowCrossTenantGetter(x))
            .Must((_, val) => val is null || !val.Value || parentEffective.AllowCrossTenant)
            .When(x => allowCrossTenantGetter(x) is not null)
            .WithMessage("AllowCrossTenant cannot be set to true when parent effective is false");
    }

    private static bool IsStepFailurePolicyValid(string? childValue, string parentValue)
    {
        if (childValue is null) return true;
        // ContinueStage is less strict than HaltStage — reject only this combination.
        return !(string.Equals(childValue, "ContinueStage", System.StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(parentValue, "HaltStage", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsStageFailurePolicyValid(string? childValue, string parentValue)
    {
        if (childValue is null) return true;
        // ContinueProject is less strict than HaltProject — reject only this combination.
        return !(string.Equals(childValue, "ContinueProject", System.StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(parentValue, "HaltProject", System.StringComparison.OrdinalIgnoreCase));
    }
}
