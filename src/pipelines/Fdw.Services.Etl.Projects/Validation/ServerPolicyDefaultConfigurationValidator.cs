using System;
using FluentValidation;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>
/// Validator for <see cref="ServerPolicyDefaultConfiguration"/> (FDW-4052): the server's execution policy defaults state every policy,
/// each one a value the orchestrator reads.
/// </summary>
/// <remarks>
/// Why every policy is required here when a project's are not: a project, stage, step or node leaves a policy out to inherit it, and
/// this row is what the root inherits from (EffectivePolicyResolver.ServerSnapshot). Nothing stands behind it, so nothing is defaulted.
/// The three booleans are always stated by their columns (NOT NULL, no DEFAULT). The policy names are the ones
/// ProjectConfigurationValidator and the orchestrator read.
/// </remarks>
public sealed class ServerPolicyDefaultConfigurationValidator : FdwConfigurationValidator<ServerPolicyDefaultConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerPolicyDefaultConfigurationValidator"/> class.
    /// </summary>
    public ServerPolicyDefaultConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(256)
            .WithMessage("Name must not exceed 256 characters");

        RuleFor(x => x.Domain)
            .NotEmpty()
            .WithMessage("Domain is required")
            .MaximumLength(100)
            .WithMessage("Domain must not exceed 100 characters");

        RuleFor(x => x.Implementation)
            .NotEmpty()
            .WithMessage("Implementation is required")
            .MaximumLength(100)
            .WithMessage("Implementation must not exceed 100 characters");

        RuleFor(x => x.StepFailurePolicy)
            .NotEmpty()
            .WithMessage("StepFailurePolicy is required: HaltStage or ContinueStage")
            .Must(policy => string.IsNullOrEmpty(policy)
                || string.Equals(policy, "HaltStage", StringComparison.OrdinalIgnoreCase)
                || string.Equals(policy, "ContinueStage", StringComparison.OrdinalIgnoreCase))
            .WithMessage(x => $"StepFailurePolicy '{x.StepFailurePolicy}' is not HaltStage or ContinueStage");

        RuleFor(x => x.StageFailurePolicy)
            .NotEmpty()
            .WithMessage("StageFailurePolicy is required: HaltProject or ContinueProject")
            .Must(policy => string.IsNullOrEmpty(policy)
                || string.Equals(policy, "HaltProject", StringComparison.OrdinalIgnoreCase)
                || string.Equals(policy, "ContinueProject", StringComparison.OrdinalIgnoreCase))
            .WithMessage(x => $"StageFailurePolicy '{x.StageFailurePolicy}' is not HaltProject or ContinueProject");

        RuleFor(x => x.MaxParallelPipelines)
            .GreaterThan(0)
            .WithMessage("MaxParallelPipelines must be greater than 0");

        RuleFor(x => x.ResiliencyPolicyId)
            .Must(id => id is not { } stated || stated != Guid.Empty)
            .WithMessage("ResiliencyPolicyId must not be an empty GUID when it is stated");
    }
}
