using System;
using FluentValidation;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>
/// FluentValidation validator for <see cref="OrchestrationNodeConfiguration"/>.
/// Validates name, NodeTypeId, parent constraints, policy fields, and cross-tenant rules.
/// </summary>
public sealed class OrchestrationNodeConfigurationValidator : AbstractValidator<OrchestrationNodeConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationNodeConfigurationValidator"/> class.
    /// </summary>
    /// <param name="serverDefaults">Server-level policy defaults used to validate elevation direction.</param>
    public OrchestrationNodeConfigurationValidator(IServerPolicyDefaults serverDefaults)
    {
        if (serverDefaults == null) throw new ArgumentNullException(nameof(serverDefaults));

        RegisterBasicRules();
        RegisterNodeTypeRules();
        RegisterPolicyRules(serverDefaults);
        RegisterResiliencyRules();
    }

    private void RegisterBasicRules()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("OrchestrationNode name is required.")
            .MaximumLength(200)
            .WithMessage("OrchestrationNode name must not exceed 200 characters.");

        RuleFor(x => x.Ordinal)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Ordinal must be >= 0.");
    }

    private void RegisterNodeTypeRules()
    {
        // NodeTypeId must resolve to a known OrchestrationNodeType.
        RuleFor(x => x.NodeTypeId)
            .Must(id =>
            {
                var type = OrchestrationNodeTypes.ById(id);
                return type != OrchestrationNodeTypes.NotFound;
            })
            .WithMessage(x => $"NodeTypeId '{x.NodeTypeId}' does not resolve to a registered OrchestrationNodeType.");

        // Non-root types must have a parent. Why: validate the LOGICAL ParentId — RowId is DB-managed and
        // invisible; the parent link the caller supplies is the durable ParentId.
        RuleFor(x => x.ParentId)
            .Must((config, domainConfigurationId) =>
            {
                var nodeType = OrchestrationNodeTypes.ById(config.NodeTypeId);
                if (nodeType == OrchestrationNodeTypes.NotFound) return true; // covered above
                return nodeType.CanBeRoot || domainConfigurationId.HasValue;
            })
            .WithMessage(x =>
            {
                var nodeType = OrchestrationNodeTypes.ById(x.NodeTypeId);
                return $"Node '{x.Name}' with type '{nodeType.Name}' (CanBeRoot=false) must have a non-null ParentId.";
            });
    }

    private void RegisterPolicyRules(IServerPolicyDefaults serverDefaults)
    {
        // StepFailurePolicy — valid values.
        RuleFor(x => x.StepFailurePolicy)
            .Must(val => val is null ||
                         string.Equals(val, "HaltStage", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(val, "ContinueStage", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StepFailurePolicy is not null)
            .WithMessage(x => $"Invalid StepFailurePolicy '{x.StepFailurePolicy}'. Valid values: 'HaltStage', 'ContinueStage'.");

        // Cannot relax StepFailurePolicy below server default.
        RuleFor(x => x.StepFailurePolicy)
            .Must(val => val is null ||
                         !string.Equals(val, "ContinueStage", StringComparison.OrdinalIgnoreCase) ||
                         !string.Equals(serverDefaults.StepFailurePolicy, "HaltStage", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StepFailurePolicy is not null)
            .WithMessage("StepFailurePolicy 'ContinueStage' is less strict than server default 'HaltStage'.");

        // StageFailurePolicy — valid values.
        RuleFor(x => x.StageFailurePolicy)
            .Must(val => val is null ||
                         string.Equals(val, "HaltProject", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(val, "ContinueProject", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StageFailurePolicy is not null)
            .WithMessage(x => $"Invalid StageFailurePolicy '{x.StageFailurePolicy}'. Valid values: 'HaltProject', 'ContinueProject'.");

        // Cannot relax StageFailurePolicy below server default.
        RuleFor(x => x.StageFailurePolicy)
            .Must(val => val is null ||
                         !string.Equals(val, "ContinueProject", StringComparison.OrdinalIgnoreCase) ||
                         !string.Equals(serverDefaults.StageFailurePolicy, "HaltProject", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StageFailurePolicy is not null)
            .WithMessage("StageFailurePolicy 'ContinueProject' is less strict than server default 'HaltProject'.");

        // MaxParallelPipelines: must be positive and <= server default.
        RuleFor(x => x.MaxParallelPipelines)
            .GreaterThan(0)
            .When(x => x.MaxParallelPipelines is not null)
            .WithMessage("MaxParallelPipelines must be > 0 when set.");

        RuleFor(x => x.MaxParallelPipelines)
            .Must(val => val is null || val.Value <= serverDefaults.MaxParallelPipelines)
            .When(x => x.MaxParallelPipelines is not null)
            .WithMessage(x => $"MaxParallelPipelines {x.MaxParallelPipelines} exceeds server default {serverDefaults.MaxParallelPipelines}.");

        // RequireApprovalToRun: may set true; may not set false if server default is true.
        RuleFor(x => x.RequireApprovalToRun)
            .Must(val => val is null || val.Value || !serverDefaults.RequireApprovalToRun)
            .When(x => x.RequireApprovalToRun is not null)
            .WithMessage("RequireApprovalToRun cannot be false when server default is true.");

        // AllowResume: may set false; may not set true if server default is false.
        RuleFor(x => x.AllowResume)
            .Must(val => val is null || !val.Value || serverDefaults.AllowResume)
            .When(x => x.AllowResume is not null)
            .WithMessage("AllowResume cannot be true when server default is false.");

        // AllowCrossTenant: may set false; may not set true if server default is false.
        RuleFor(x => x.AllowCrossTenant)
            .Must(val => val is null || !val.Value || serverDefaults.AllowCrossTenant)
            .When(x => x.AllowCrossTenant is not null)
            .WithMessage("AllowCrossTenant cannot be true when server default is false.");
    }

    private void RegisterResiliencyRules()
    {
        // ResiliencyPolicyId: if set, must not be the empty GUID.
        RuleFor(x => x.ResiliencyPolicyId)
            .Must(val => val is null || val.Value != Guid.Empty)
            .When(x => x.ResiliencyPolicyId is not null)
            .WithMessage("ResiliencyPolicyId must not be an empty GUID when specified. Set to null to inherit.");
    }
}
