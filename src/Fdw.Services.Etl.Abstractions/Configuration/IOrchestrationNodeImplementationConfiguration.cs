using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>The contract every OrchestrationNode implementation carries.</summary>
public interface IOrchestrationNodeImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid OrchestrationNodeId { get; set; }

    /// <summary>Gets or sets the OrchestrationNode NodeTypeId.</summary>
    int NodeTypeId { get; set; }

    /// <summary>Gets or sets the OrchestrationNode ParentId.</summary>
    Guid? ParentId { get; set; }

    /// <summary>Gets or sets the OrchestrationNode Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the OrchestrationNode Ordinal.</summary>
    int Ordinal { get; set; }

    /// <summary>Gets or sets the OrchestrationNode IsEnabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets the OrchestrationNode TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the OrchestrationNode VisibilityGroupId.</summary>
    Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the OrchestrationNode StepFailurePolicy.</summary>
    string? StepFailurePolicy { get; set; }

    /// <summary>Gets or sets the OrchestrationNode StageFailurePolicy.</summary>
    string? StageFailurePolicy { get; set; }

    /// <summary>Gets or sets the OrchestrationNode MaxParallelPipelines.</summary>
    int? MaxParallelPipelines { get; set; }

    /// <summary>Gets or sets the OrchestrationNode RequireApprovalToRun.</summary>
    bool? RequireApprovalToRun { get; set; }

    /// <summary>Gets or sets the OrchestrationNode AllowResume.</summary>
    bool? AllowResume { get; set; }

    /// <summary>Gets or sets the OrchestrationNode AllowCrossTenant.</summary>
    bool? AllowCrossTenant { get; set; }

    /// <summary>Gets or sets the OrchestrationNode ResiliencyPolicyId.</summary>
    Guid? ResiliencyPolicyId { get; set; }

    /// <summary>Gets or sets the OrchestrationNode Children.</summary>
    IList<OrchestrationNodeImplementationConfiguration> Children { get; set; }

    /// <summary>Gets or sets the OrchestrationNode PipelineMemberships.</summary>
    IList<OrchestrationNodePipelineMembershipConfiguration> PipelineMemberships { get; set; }

    /// <summary>Gets or sets the OrchestrationNode PipelinePrerequisites.</summary>
    IList<OrchestrationNodePipelinePrerequisiteConfiguration> PipelinePrerequisites { get; set; }
}
