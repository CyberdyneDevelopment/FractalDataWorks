using System;
using FluentValidation;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Policy;

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
    /// <param name="serverDefaults">Reads the server's policy defaults, which a project may tighten but not relax.</param>
    public ProjectConfigurationValidator(IServerPolicyDefaultConfigurationProvider serverDefaults)
    {
        if (serverDefaults == null) throw new ArgumentNullException(nameof(serverDefaults));

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(200)
            .WithMessage("Project name must not exceed 200 characters.");

        // Policy fields: Project is the root level — validate against server defaults.
        RuleFor(x => x.StepFailurePolicy)
            .Must(val => val is null ||
                         string.Equals(val, "HaltStage", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(val, "ContinueStage", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StepFailurePolicy is not null)
            .WithMessage(x => $"Invalid StepFailurePolicy '{x.StepFailurePolicy}'. Valid values: 'HaltStage', 'ContinueStage'.");

        RuleFor(x => x.StageFailurePolicy)
            .Must(val => val is null ||
                         string.Equals(val, "HaltProject", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(val, "ContinueProject", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StageFailurePolicy is not null)
            .WithMessage(x => $"Invalid StageFailurePolicy '{x.StageFailurePolicy}'. Valid values: 'HaltProject', 'ContinueProject'.");

        // MaxParallelPipelines: must be positive (the server-default ceiling is checked with the defaults below).
        RuleFor(x => x.MaxParallelPipelines)
            .GreaterThan(0)
            .When(x => x.MaxParallelPipelines is not null)
            .WithMessage("MaxParallelPipelines must be > 0 when set.");

        // ResiliencyPolicyId: if set, must not be the empty GUID (resolved by executor at runtime).
        RuleFor(x => x.ResiliencyPolicyId)
            .Must(val => val is null || val.Value != Guid.Empty)
            .When(x => x.ResiliencyPolicyId is not null)
            .WithMessage("ResiliencyPolicyId must not be an empty GUID when specified. Set to null to inherit.");

        // Why one async rule: the server's defaults are read from their provider when a configuration is validated,
        // once per validation, and each setting is checked against them (FDW-3580). A failed read is the failure.
        RuleFor(x => x).CustomAsync(async (config, context, ct) =>
        {
            var defaults = await serverDefaults.GetCurrent(ct).ConfigureAwait(false);
            if (defaults.IsFailure || defaults.Value is not { } current)
            {
                context.AddFailure(defaults.NoValueReason().Message);
                return;
            }

            foreach (var violation in ServerPolicyElevation.Violations(
                config.StepFailurePolicy, config.StageFailurePolicy, config.MaxParallelPipelines,
                config.RequireApprovalToRun, config.AllowResume, config.AllowCrossTenant, current))
            {
                context.AddFailure(violation);
            }
        });
    }
}
