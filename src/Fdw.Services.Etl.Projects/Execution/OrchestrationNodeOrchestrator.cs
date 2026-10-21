using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Etl.Projects.Logging;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Resiliency;
using Fdw.Services.Resiliency.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Recursive orchestration engine for the OrchestrationNode hierarchy.
/// Branch nodes (CanHostPipelines=false) execute children sequentially in Ordinal order.
/// Leaf nodes (CanHostPipelines=true) execute pipeline memberships as their prerequisites allow, bounded by MaxParallelPipelines.
/// Stage-band nodes (NodeTypeId=StageNodeType.Id) additionally wrap their execution in IResiliencyExecutor.
/// </summary>
/// <remarks>
/// Flow:
/// 1. Resolve the root node via IOrchestrationNodeConfigurationProvider.Get(id) -- the provider composes the whole aggregate, so there is no depth to ask for.
/// 2. Transition root ExecutionItem → Running; broadcast.
/// 3. ExecuteNode(root): if CanHostPipelines → execute pipelines; else → foreach(child) ExecuteNode(child).
/// 4. Failure policy: ShouldHaltOnChildFailure reads StepFailurePolicy/StageFailurePolicy depending on level.
/// 5. Root completion: tracker.Complete + BroadcastNodeStatus.
/// </remarks>
public sealed class OrchestrationNodeOrchestrator : IOrchestrationNodeOrchestrator
{
    private static readonly int StageNodeTypeId = OrchestrationNodeTypes.ByName("Stage").Id;

    private readonly IOrchestrationNodeConfigurationProvider _nodeProvider;
    private readonly IEffectivePolicyResolver _policyResolver;
    private readonly IResiliencyExecutor _resiliencyExecutor;
    private readonly IExecutionTracker _tracker;
    private readonly IPipelineRunStarter _starter;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly ILogger<OrchestrationNodeOrchestrator> _logger;

    /// <summary>Initializes a new instance of <see cref="OrchestrationNodeOrchestrator"/>.</summary>
    public OrchestrationNodeOrchestrator(
        IOrchestrationNodeConfigurationProvider nodeProvider,
        IEffectivePolicyResolver policyResolver,
        IResiliencyExecutor resiliencyExecutor,
        IExecutionTracker tracker,
        IPipelineRunStarter starter,
        IPipelineStatusBroadcaster broadcaster,
        ILogger<OrchestrationNodeOrchestrator>? logger = null)
    {
        _nodeProvider = nodeProvider ?? throw new ArgumentNullException(nameof(nodeProvider));
        _policyResolver = policyResolver ?? throw new ArgumentNullException(nameof(policyResolver));
        _resiliencyExecutor = resiliencyExecutor ?? throw new ArgumentNullException(nameof(resiliencyExecutor));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _starter = starter ?? throw new ArgumentNullException(nameof(starter));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        _logger = logger ?? NullLogger<OrchestrationNodeOrchestrator>.Instance;
    }

    /// <inheritdoc/>
    public async Task Execute(OrchestrationNodeExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var configResult = await _nodeProvider.Get(request.RootNodeId, cancellationToken)
            .ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            OrchestrationNodeOrchestratorLog.NodeNotFound(_logger, request.RootNodeId);
            await CompleteRootExecution(request, nodeName: request.RootNodeId.ToString("N"), succeeded: false)
                .ConfigureAwait(false);
            return;
        }

        var rootNode = configResult.Value;
        OrchestrationNodeOrchestratorLog.NodeExecutionStarted(
            _logger, rootNode.Name, rootNode.NodeTypeId.ToString(System.Globalization.CultureInfo.InvariantCulture), request.ExecutionId, request.TriggerSource);

        // Transition root to Running.
        var rootTransitionResult = await _tracker.TransitionState(
            request.ExecutionId,
            ExecutionStateTypes.Running,
            "Orchestration started",
            actor: "OrchestrationNodeOrchestrator",
            cancellationToken).ConfigureAwait(false);
        if (!rootTransitionResult.IsSuccess)
        {
            OrchestrationNodeOrchestratorLog.RootTransitionStateFailed(_logger, request.ExecutionId, rootTransitionResult.CurrentMessage);
        }

        await _broadcaster.BroadcastStatusChange(rootNode.Name, request.ExecutionId, "Running", orgId: null)
            .ConfigureAwait(false);

        // The root inherits from the server defaults.
        var rootPolicy = _policyResolver.ResolveForRootNode(rootNode);

        // Recursive execution.
        var overallSuccess = await ExecuteNode(
            rootNode, rootPolicy, request.ExecutionId, new RunScope(request.TenantId, request.RunRoles), cancellationToken)
            .ConfigureAwait(false);

        var finalStatus = overallSuccess ? "Succeeded" : "Failed";
        OrchestrationNodeOrchestratorLog.NodeExecutionCompleted(
            _logger, rootNode.Name, request.ExecutionId, finalStatus);

        await CompleteRootExecution(request, rootNode.Name, overallSuccess).ConfigureAwait(false);
    }

    /// <summary>
    /// Recursively executes a node:
    /// - If CanHostPipelines: run pipeline memberships in parallel (leaf behavior).
    /// - Else: run children sequentially in Ordinal order, applying failure policies (branch behavior).
    /// Stage-level nodes additionally wrap their children in IResiliencyExecutor.
    /// </summary>
    private async Task<bool> ExecuteNode(
        IOrchestrationNodeImplementationConfiguration node,
        ExecutionPolicySnapshot effectivePolicy,
        Guid parentExecutionItemId,
        RunScope scope,
        CancellationToken cancellationToken)
    {
        var nodeType = OrchestrationNodeTypes.ById(node.NodeTypeId);

        if (nodeType.CanHostPipelines)
        {
            // Leaf: execute pipeline memberships.
            return await ExecuteLeafNode(node, effectivePolicy, parentExecutionItemId, scope, cancellationToken)
                .ConfigureAwait(false);
        }

        // Branch: execute children sequentially.
        var overallSuccess = true;
        var children = node.Children.OrderBy(c => c.Ordinal).ToList();

        foreach (var child in children)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var childPolicy = _policyResolver.ResolveForNode(child, effectivePolicy);

            // Create a tracking item for this child.
            // A child belongs to its parent's tenant, so it states none of its own.
            var childItemResult = await _tracker.CreateItem(
                ExecutionItemTypes.ByName(nodeType.Name),
                child.Name,
                tenantId: null,
                parentId: parentExecutionItemId,
                correlationId: parentExecutionItemId.ToString("N"),
                triggerSource: $"Node:{node.Name}",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!childItemResult.IsSuccess || childItemResult.Value is null)
            {
                overallSuccess = false;
                break;
            }

            var childItem = childItemResult.Value;
            var childTransitionResult = await _tracker.TransitionState(
                childItem.Id, ExecutionStateTypes.Running,
                "Child node execution started", actor: "OrchestrationNodeOrchestrator", cancellationToken)
                .ConfigureAwait(false);
            if (!childTransitionResult.IsSuccess)
            {
                OrchestrationNodeOrchestratorLog.ChildTransitionStateFailed(_logger, childItem.Id, childTransitionResult.CurrentMessage);
            }

            OrchestrationNodeOrchestratorLog.ChildNodeExecutionStarted(_logger, child.Name, child.Ordinal, childItem.Id);

            var childSucceeded = await ExecuteChildNode(child, childPolicy, parentExecutionItemId, childItem.Id, scope, cancellationToken)
                .ConfigureAwait(false);

            var childCompleteResult = await _tracker.Complete(
                childItem.Id, childSucceeded,
                childSucceeded ? "Succeeded" : "Failed",
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
            if (!childCompleteResult.IsSuccess)
            {
                OrchestrationNodeOrchestratorLog.ChildCompleteFailed(_logger, childItem.Id, childCompleteResult.CurrentMessage);
            }

            if (!childSucceeded)
            {
                OrchestrationNodeOrchestratorLog.ChildNodeExecutionFailed(_logger, child.Name, child.Ordinal, parentExecutionItemId);

                if (ShouldHaltOnChildFailure(effectivePolicy, child))
                {
                    OrchestrationNodeOrchestratorLog.HaltingDueToPolicy(
                        _logger, GetHaltPolicy(effectivePolicy, child), node.Name, parentExecutionItemId);
                    overallSuccess = false;
                    break;
                }

                overallSuccess = false;
                // Continue: mark degraded but keep running remaining siblings.
            }
        }

        return overallSuccess;
    }

    /// <summary>
    /// Executes a single child node, optionally wrapped in IResiliencyExecutor for Stage-band nodes.
    /// Extracted from ExecuteNode to satisfy FDW006 (60-line method limit).
    /// </summary>
    private async Task<bool> ExecuteChildNode(
        OrchestrationNodeImplementationConfiguration child,
        ExecutionPolicySnapshot childPolicy,
        Guid parentExecutionItemId,
        Guid childItemId,
        RunScope scope,
        CancellationToken cancellationToken)
    {
        if (child.NodeTypeId == StageNodeTypeId && childPolicy.ResiliencyPolicyId.HasValue)
        {
            var resiliencyCtx = new ResiliencyExecutionContext
            {
                ExecutionId = parentExecutionItemId,
                StageId = childItemId
            };

            var resiliencyResult = await _resiliencyExecutor.Execute(
                childPolicy.ResiliencyPolicyId,
                async ct =>
                {
                    var nodeSucceeded = await ExecuteNode(child, childPolicy, childItemId, scope, ct).ConfigureAwait(false);
                    return nodeSucceeded
                        ? GenericResult.Success()
                        : GenericResult.Failure(OrchestrationNodeOrchestratorLog.NodeFailed(_logger, childItemId));
                },
                resiliencyCtx,
                cancellationToken).ConfigureAwait(false);

            return resiliencyResult.IsSuccess;
        }

        return await ExecuteNode(child, childPolicy, childItemId, scope, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a leaf node by running its pipeline memberships as their prerequisites allow,
    /// never more than MaxParallelPipelines in the effective policy at once.
    /// </summary>
    private async Task<bool> ExecuteLeafNode(
        IOrchestrationNodeImplementationConfiguration node,
        ExecutionPolicySnapshot effectivePolicy,
        Guid parentExecutionItemId,
        RunScope scope,
        CancellationToken cancellationToken)
    {
        if (node.PipelineMemberships.Count == 0)
        {
            return true;
        }

        if (scope.TenantId is not { } tenantId)
        {
            PipelineRunStartLog.OrchestrationHasNoTenant(_logger, node.Name);
            return false;
        }

        var scheduled = await PipelinePrerequisiteScheduler.Execute(
            _logger,
            node.Name,
            node.PipelineMemberships.OrderBy(p => p.Ordinal).Select(p => p.PipelineId).ToList(),
            node.PipelinePrerequisites,
            effectivePolicy.MaxParallelPipelines,
            (pipelineId, ct) => RunPipeline(node, pipelineId, parentExecutionItemId, tenantId, scope, ct),
            cancellationToken).ConfigureAwait(false);

        return scheduled.IsSuccess && scheduled.Value;
    }

    // Started under this node's execution item: its child, in its tenant, with its correlation, acting with its roles.
    private async Task<bool> RunPipeline(
        IOrchestrationNodeImplementationConfiguration node,
        Guid pipelineId,
        Guid parentExecutionItemId,
        Guid tenantId,
        RunScope scope,
        CancellationToken cancellationToken)
    {
        var started = await _starter.Start(
            new PipelineRunStart
            {
                ParentExecutionId = parentExecutionItemId,
                PipelineId = pipelineId,
                TenantId = tenantId,
                RunRoles = scope.RunRoles,
                TriggerSource = $"Node:{node.Name}",
                Parameters = [],
                AwaitCompletion = true
            }, cancellationToken).ConfigureAwait(false);
        if (started.IsFailure)
        {
            return false;
        }

        OrchestrationNodeOrchestratorLog.PipelineDispatched(_logger, node.Name, pipelineId, started.Value);

        var pipelineSucceeded = await _starter.Await(started.Value, cancellationToken).ConfigureAwait(false);
        OrchestrationNodeOrchestratorLog.PipelineCompletionReceived(_logger, started.Value, pipelineSucceeded);
        return pipelineSucceeded;
    }

    /// <summary>
    /// Determines whether execution should halt after a child node failure,
    /// based on which failure policy is applicable at the current parent level.
    /// </summary>
    private static bool ShouldHaltOnChildFailure(
        ExecutionPolicySnapshot parentPolicy,
        OrchestrationNodeImplementationConfiguration failedChild)
    {
        var nodeType = OrchestrationNodeTypes.ById(failedChild.NodeTypeId);
        if (nodeType.Name.Equals("Step", StringComparison.Ordinal) ||
            nodeType.CanHostPipelines)
        {
            return string.Equals(parentPolicy.StepFailurePolicy, "HaltStage", StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(parentPolicy.StageFailurePolicy, "HaltProject", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns the name of the halting policy for logging purposes.</summary>
    private static string GetHaltPolicy(
        ExecutionPolicySnapshot parentPolicy,
        OrchestrationNodeImplementationConfiguration failedChild)
    {
        var nodeType = OrchestrationNodeTypes.ById(failedChild.NodeTypeId);
        return (nodeType.Name.Equals("Step", StringComparison.Ordinal) || nodeType.CanHostPipelines)
            ? parentPolicy.StepFailurePolicy
            : parentPolicy.StageFailurePolicy;
    }

    /// <summary>Completes the root execution item and broadcasts final status.</summary>
    private async Task CompleteRootExecution(
        OrchestrationNodeExecutionRequest request,
        string nodeName,
        bool succeeded)
    {
        var rootCompleteResult = await _tracker.Complete(
            request.ExecutionId, succeeded,
            succeeded ? "Succeeded" : "Failed",
            cancellationToken: CancellationToken.None).ConfigureAwait(false);
        if (!rootCompleteResult.IsSuccess)
        {
            OrchestrationNodeOrchestratorLog.RootCompleteFailed(_logger, request.ExecutionId, rootCompleteResult.CurrentMessage);
        }

        await _broadcaster.BroadcastStatusChange(
            nodeName, request.ExecutionId, succeeded ? "Succeeded" : "Failed", orgId: null)
            .ConfigureAwait(false);
    }

    // What a started pipeline belongs to: the tenant of the orchestration run and the roles it acts with.
    private sealed record RunScope(Guid? TenantId, IReadOnlyList<string> RunRoles);
}
