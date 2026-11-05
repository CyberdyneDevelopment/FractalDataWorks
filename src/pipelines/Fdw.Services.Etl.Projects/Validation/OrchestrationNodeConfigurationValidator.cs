using System;
using System.Linq;
using FluentValidation;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Policy;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>
/// FluentValidation validator for <see cref="OrchestrationNodeImplementationConfiguration"/>.
/// Validates name, NodeTypeId, parent constraints, policy fields, and cross-tenant rules.
/// </summary>
public sealed class OrchestrationNodeConfigurationValidator : AbstractValidator<OrchestrationNodeImplementationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationNodeConfigurationValidator"/> class.
    /// </summary>
    /// <param name="serverDefaults">Reads the server's policy defaults, which a node may tighten but not relax.</param>
    public OrchestrationNodeConfigurationValidator(IServerPolicyDefaultConfigurationProvider serverDefaults)
    {
        if (serverDefaults == null) throw new ArgumentNullException(nameof(serverDefaults));

        RegisterBasicRules();
        RegisterNodeTypeRules();
        RegisterPolicyRules(serverDefaults);
        RegisterResiliencyRules();
        RegisterPrerequisiteRules();
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
            .Must((config, parentId) =>
            {
                var nodeType = OrchestrationNodeTypes.ById(config.NodeTypeId);
                if (nodeType == OrchestrationNodeTypes.NotFound) return true; // covered above
                return nodeType.CanBeRoot || parentId.HasValue;
            })
            .WithMessage(x =>
            {
                var nodeType = OrchestrationNodeTypes.ById(x.NodeTypeId);
                return $"Node '{x.Name}' with type '{nodeType.Name}' (CanBeRoot=false) must have a non-null ParentId.";
            });
    }

    private void RegisterPolicyRules(IServerPolicyDefaultConfigurationProvider serverDefaults)
    {
        // StepFailurePolicy — valid values.
        RuleFor(x => x.StepFailurePolicy)
            .Must(val => val is null ||
                         string.Equals(val, "HaltStage", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(val, "ContinueStage", StringComparison.OrdinalIgnoreCase))
            .When(x => x.StepFailurePolicy is not null)
            .WithMessage(x => $"Invalid StepFailurePolicy '{x.StepFailurePolicy}'. Valid values: 'HaltStage', 'ContinueStage'.");

        // StageFailurePolicy — valid values.
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

    // The pipelines of a prerequisite must both be members of the node, and the prerequisites must not form a cycle (a pipeline
    // that needs itself is one). The cycle test is the one the run-time scheduler uses, so a graph that saves can be run.
    private void RegisterPrerequisiteRules()
    {
        RuleForEach(x => x.PipelinePrerequisites)
            .Must((config, prerequisite) =>
                config.PipelineMemberships.Any(m => m.PipelineId == prerequisite.PipelineId) &&
                config.PipelineMemberships.Any(m => m.PipelineId == prerequisite.PrerequisitePipelineId))
            .WithMessage((config, prerequisite) =>
                $"Node '{config.Name}': prerequisite '{Describe(config, prerequisite)}' names a pipeline the node does not hold, and both pipelines of a prerequisite must be members of the node.");

        RuleFor(x => x.PipelinePrerequisites)
            .Must((config, prerequisites) =>
                PipelinePrerequisiteGraph.FindCyclic(config.PipelineMemberships.Select(m => m.PipelineId), prerequisites).Count == 0)
            .WithMessage(config =>
                $"Node '{config.Name}': the prerequisites form a cycle through pipeline '{Label(config, PipelinePrerequisiteGraph.FindCyclic(config.PipelineMemberships.Select(m => m.PipelineId), config.PipelinePrerequisites)[0])}'.");
    }

    // Names a prerequisite for the person who sent the node: the name it was saved with, or the two pipelines it joins.
    private static string Describe(OrchestrationNodeImplementationConfiguration config, OrchestrationNodePipelinePrerequisiteConfiguration prerequisite)
        => prerequisite.Name.Length > 0
            ? prerequisite.Name
            : $"{Label(config, prerequisite.PipelineId)} needs {Label(config, prerequisite.PrerequisitePipelineId)}";

    // Names a pipeline for the person who sent the node: the name its membership was saved with, or its id when that membership has no name
    // (a pipeline the node does not hold has no membership to name it).
    private static string Label(OrchestrationNodeImplementationConfiguration config, Guid pipelineId)
    {
        var membership = config.PipelineMemberships.FirstOrDefault(m => m.PipelineId == pipelineId);
        return membership is { Name.Length: > 0 } ? membership.Name : pipelineId.ToString();
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
