using System;
using FluentValidation;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>
/// FluentValidation validator for <see cref="ProjectConfiguration"/>.
/// Validates name, policies against server defaults, and resiliency policy ID format.
/// </summary>
public sealed class ProjectConfigurationValidator : AbstractValidator<ProjectConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectConfigurationValidator"/> class.
    /// </summary>
    /// <param name="serverDefaults">Server-level policy defaults used to validate elevation direction.</param>
    public ProjectConfigurationValidator(IServerPolicyDefaults serverDefaults)
    {
        if (serverDefaults == null) throw new ArgumentNullException(nameof(serverDefaults));

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(200)
            .WithMessage("Project name must not exceed 200 characters.");

        // Policy fields: Project is the root level — validate against server defaults.
        // Why: Project can be more strict than server defaults; it can never be less strict.
        RuleFor(x => x.StepFailurePolicy)
            .Must(val => val is null ||
                         string.Equals(val, "HaltStage", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(val, "ContinueStage", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StepFailurePolicy is not null)
            .WithMessage(x => $"Invalid StepFailurePolicy '{x.StepFailurePolicy}'. Valid values: 'HaltStage', 'ContinueStage'.");

        // Cannot set ContinueStage when server default is HaltStage.
        RuleFor(x => x.StepFailurePolicy)
            .Must(val => val is null ||
                         !string.Equals(val, "ContinueStage", StringComparison.OrdinalIgnoreCase) ||
                         !string.Equals(serverDefaults.StepFailurePolicy, "HaltStage", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StepFailurePolicy is not null)
            .WithMessage(x => $"StepFailurePolicy 'ContinueStage' is less strict than server default 'HaltStage'.");

        RuleFor(x => x.StageFailurePolicy)
            .Must(val => val is null ||
                         string.Equals(val, "HaltProject", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(val, "ContinueProject", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StageFailurePolicy is not null)
            .WithMessage(x => $"Invalid StageFailurePolicy '{x.StageFailurePolicy}'. Valid values: 'HaltProject', 'ContinueProject'.");

        // Cannot set ContinueProject when server default is HaltProject.
        RuleFor(x => x.StageFailurePolicy)
            .Must(val => val is null ||
                         !string.Equals(val, "ContinueProject", StringComparison.OrdinalIgnoreCase) ||
                         !string.Equals(serverDefaults.StageFailurePolicy, "HaltProject", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StageFailurePolicy is not null)
            .WithMessage(x => $"StageFailurePolicy 'ContinueProject' is less strict than server default 'HaltProject'.");

        // MaxParallelPipelines: must be positive and <= server default.
        RuleFor(x => x.MaxParallelPipelines)
            .GreaterThan(0)
            .When(x => x.MaxParallelPipelines is not null)
            .WithMessage("MaxParallelPipelines must be > 0 when set.");

        RuleFor(x => x.MaxParallelPipelines)
            .Must(val => val is null || val.Value <= serverDefaults.MaxParallelPipelines)
            .When(x => x.MaxParallelPipelines is not null)
            .WithMessage(x => $"MaxParallelPipelines {x.MaxParallelPipelines} exceeds server default {serverDefaults.MaxParallelPipelines}.");

        // RequireApprovalToRun: project may set true; may not set false if server default is true.
        RuleFor(x => x.RequireApprovalToRun)
            .Must(val => val is null || val.Value || !serverDefaults.RequireApprovalToRun)
            .When(x => x.RequireApprovalToRun is not null)
            .WithMessage("RequireApprovalToRun cannot be false when server default is true.");

        // AllowResume: project may set false; may not set true if server default is false.
        RuleFor(x => x.AllowResume)
            .Must(val => val is null || !val.Value || serverDefaults.AllowResume)
            .When(x => x.AllowResume is not null)
            .WithMessage("AllowResume cannot be true when server default is false.");

        // AllowCrossTenant: project may set false; may not set true if server default is false.
        RuleFor(x => x.AllowCrossTenant)
            .Must(val => val is null || !val.Value || serverDefaults.AllowCrossTenant)
            .When(x => x.AllowCrossTenant is not null)
            .WithMessage("AllowCrossTenant cannot be true when server default is false.");

        // ResiliencyPolicyId: if set, must not be the empty GUID (resolved by executor at runtime).
        RuleFor(x => x.ResiliencyPolicyId)
            .Must(val => val is null || val.Value != Guid.Empty)
            .When(x => x.ResiliencyPolicyId is not null)
            .WithMessage("ResiliencyPolicyId must not be an empty GUID when specified. Set to null to inherit.");
    }
}
