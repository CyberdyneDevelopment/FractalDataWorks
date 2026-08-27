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
/// Leaf nodes (CanHostPipelines=true) execute pipeline memberships in parallel bounded by MaxParallelPipelines.
/// Stage-band nodes (NodeTypeId=StageNodeType.Id) additionally wrap their execution in IResiliencyExecutor.
/// </summary>
/// <remarks>
/// Flow:
/// 1. Resolve root OrchestrationNodeConfiguration via IOrchestrationNodeConfigurationProvider.Get(id, depth).
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
    private readonly IPipelineExecutionQueue _pipelineQueue;
    private readonly IExecutionCompletionSignaler _signaler;
    private readonly IPipelineStatusBroadcaster _broadcaster;
    private readonly IServerPolicyDefaults _serverDefaults;
    private readonly ILogger<OrchestrationNodeOrchestrator> _logger;

    /// <summary>Initializes a new instance of <see cref="OrchestrationNodeOrchestrator"/>.</summary>
    public OrchestrationNodeOrchestrator(
        IOrchestrationNodeConfigurationProvider nodeProvider,
        IEffectivePolicyResolver policyResolver,
        IResiliencyExecutor resiliencyExecutor,
        IExecutionTracker tracker,
        IPipelineExecutionQueue pipelineQueue,
        IExecutionCompletionSignaler signaler,
        IPipelineStatusBroadcaster broadcaster,
        IServerPolicyDefaults serverDefaults,
        ILogger<OrchestrationNodeOrchestrator>? logger = null)
    {
        _nodeProvider = nodeProvider ?? throw new ArgumentNullException(nameof(nodeProvider));
        _policyResolver = policyResolver ?? throw new ArgumentNullException(nameof(policyResolver));
        _resiliencyExecutor = resiliencyExecutor ?? throw new ArgumentNullException(nameof(resiliencyExecutor));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _pipelineQueue = pipelineQueue ?? throw new ArgumentNullException(nameof(pipelineQueue));
        _signaler = signaler ?? throw new ArgumentNullException(nameof(signaler));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        _serverDefaults = serverDefaults ?? throw new ArgumentNullException(nameof(serverDefaults));
        // Why NullLogger fallback: per FDW convention, ensures the orchestrator remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<OrchestrationNodeOrchestrator>.Instance;
    }

    /// <inheritdoc/>
    public async Task Execute(OrchestrationNodeExecutionRequest request, CancellationToken cancellationToken = default)
    {
        // Why depth=int.MaxValue: load the entire subtree in one call to avoid N+1 queries
        // during recursive execution. Orchestration trees are bounded by design.
        var configResult = await _nodeProvider.Get(request.RootNodeId, depth: int.MaxValue, cancellationToken)
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

        // Why: orgId null — project-level (orchestration node) status has no owning-org firehose wired
        // yet (it would derive from the project's owning org, mirroring the pipeline path); the
        // execution:{id} group still delivers to subscribers. Per-org project firehose is a follow-up.
        await _broadcaster.BroadcastStatusChange(rootNode.Name, request.ExecutionId, "Running", orgId: null)
            .ConfigureAwait(false);

        // Build root policy from server defaults (root inherits from server).
        var serverDefaultPolicy = BuildServerDefaultPolicy();
        var rootPolicy = _policyResolver.ResolveForNode(rootNode, serverDefaultPolicy);

        // Recursive execution.
        var overallSuccess = await ExecuteNode(rootNode, rootPolicy, request.ExecutionId, cancellationToken)
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
        OrchestrationNodeConfiguration node,
        ExecutionPolicySnapshot effectivePolicy,
        Guid parentExecutionItemId,
        CancellationToken cancellationToken)
    {
        var nodeType = OrchestrationNodeTypes.ById(node.NodeTypeId);

        if (nodeType.CanHostPipelines)
        {
            // Leaf: execute pipeline memberships.
            return await ExecuteLeafNode(node, effectivePolicy, parentExecutionItemId, cancellationToken)
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
            var childItemResult = await _tracker.CreateItem(
                ExecutionItemTypes.ByName(nodeType.Name),
                child.Name,
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

            var childSucceeded = await ExecuteChildNode(child, childPolicy, parentExecutionItemId, childItem.Id, cancellationToken)
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
        OrchestrationNodeConfiguration child,
        ExecutionPolicySnapshot childPolicy,
        Guid parentExecutionItemId,
        Guid childItemId,
        CancellationToken cancellationToken)
    {
        // Why stage-level resiliency wrap: the IResiliencyExecutor wraps Stage-band nodes.
        // This matches v1 behavior where stage execution was wrapped for retry/fallback.
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
                    var nodeSucceeded = await ExecuteNode(child, childPolicy, childItemId, ct).ConfigureAwait(false);
                    return nodeSucceeded
                        ? GenericResult.Success()
                        : GenericResult.Failure(OrchestrationNodeOrchestratorLog.NodeFailed(_logger, childItemId));
                },
                resiliencyCtx,
                cancellationToken).ConfigureAwait(false);

            return resiliencyResult.IsSuccess;
        }

        return await ExecuteNode(child, childPolicy, childItemId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a leaf node by running its pipeline memberships in parallel,
    /// bounded by MaxParallelPipelines in the effective policy.
    /// </summary>
    private async Task<bool> ExecuteLeafNode(
        OrchestrationNodeConfiguration node,
        ExecutionPolicySnapshot effectivePolicy,
        Guid parentExecutionItemId,
        CancellationToken cancellationToken)
    {
        if (node.PipelineMemberships.Count == 0)
        {
            // Why: an empty leaf node succeeds trivially — nothing to run.
            return true;
        }

        // Build topological order from prerequisites.
        var orderedPipelineIds = BuildTopologicalOrder(node);

        var stepSuccess = true;
        var results = new System.Collections.Concurrent.ConcurrentBag<bool>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = effectivePolicy.MaxParallelPipelines,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(orderedPipelineIds, parallelOptions, async (pipelineId, ct) =>
        {
            var childItemResult = await _tracker.CreateItem(
                ExecutionItemTypes.ByName("Task"),
                $"Pipeline:{pipelineId}",
                parentId: parentExecutionItemId,
                correlationId: parentExecutionItemId.ToString("N"),
                triggerSource: $"Node:{node.Name}",
                cancellationToken: ct).ConfigureAwait(false);

            if (!childItemResult.IsSuccess || childItemResult.Value is null)
            {
                results.Add(false);
                return;
            }

            var childExecutionId = childItemResult.Value.Id;
            _signaler.Register(childExecutionId);

            var membership = node.PipelineMemberships.FirstOrDefault(p => p.PipelineId == pipelineId);
            var pipelineName = membership?.Name ?? pipelineId.ToString("N");

            OrchestrationNodeOrchestratorLog.PipelineDispatched(_logger, node.Name, pipelineId, childExecutionId);

            var enqueueResult = await _pipelineQueue.Enqueue(
                new PipelineExecutionRequest
                {
                    ExecutionId = childExecutionId,
                    PipelineName = pipelineName,
                    TriggerSource = $"Node:{node.Name}"
                }, ct).ConfigureAwait(false);

            if (!enqueueResult)
            {
                _signaler.Deregister(childExecutionId);
                OrchestrationNodeOrchestratorLog.OrchestratorQueueFull(_logger, pipelineName);
                results.Add(false);
                return;
            }

            bool pipelineSucceeded;
            try
            {
                pipelineSucceeded = await _signaler.Await(childExecutionId, ct).ConfigureAwait(false);
            }
            finally
            {
                // Why: always deregister to release TCS memory, success or failure.
                _signaler.Deregister(childExecutionId);
            }

            OrchestrationNodeOrchestratorLog.PipelineCompletionReceived(_logger, childExecutionId, pipelineSucceeded);
            results.Add(pipelineSucceeded);
        }).ConfigureAwait(false);

        foreach (var result in results)
        {
            if (!result)
            {
                stepSuccess = false;
                break;
            }
        }

        return stepSuccess;
    }

    /// <summary>
    /// Determines whether execution should halt after a child node failure,
    /// based on which failure policy is applicable at the current parent level.
    /// </summary>
    private static bool ShouldHaltOnChildFailure(
        ExecutionPolicySnapshot parentPolicy,
        OrchestrationNodeConfiguration failedChild)
    {
        // Why: a Step-level failure (leaf child) reads StepFailurePolicy;
        // a Stage-level failure reads StageFailurePolicy.
        // For other depths, StepFailurePolicy governs as the generic default.
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
        OrchestrationNodeConfiguration failedChild)
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

    /// <summary>
    /// Builds server-default policy snapshot as the root of the inheritance chain.
    /// </summary>
    private ExecutionPolicySnapshot BuildServerDefaultPolicy()
    {
        return new ExecutionPolicySnapshot(
            StepFailurePolicy: _serverDefaults.StepFailurePolicy,
            StageFailurePolicy: _serverDefaults.StageFailurePolicy,
            MaxParallelPipelines: _serverDefaults.MaxParallelPipelines,
            RequireApprovalToRun: _serverDefaults.RequireApprovalToRun,
            AllowResume: _serverDefaults.AllowResume,
            AllowCrossTenant: _serverDefaults.AllowCrossTenant,
            ResiliencyPolicyId: _serverDefaults.ResiliencyPolicyId);
    }

    /// <summary>
    /// Builds a topological order over the pipeline IDs in a leaf node, respecting prerequisite edges.
    /// Uses Kahn's algorithm. The node validator guarantees a DAG (no cycles).
    /// </summary>
    private static List<Guid> BuildTopologicalOrder(OrchestrationNodeConfiguration node)
    {
        var dependents = new Dictionary<Guid, List<Guid>>();
        var inDegree = new Dictionary<Guid, int>();

        foreach (var membership in node.PipelineMemberships)
        {
            if (!dependents.ContainsKey(membership.PipelineId))
                dependents[membership.PipelineId] = [];
            if (!inDegree.ContainsKey(membership.PipelineId))
                inDegree[membership.PipelineId] = 0;
        }

        foreach (var prereq in node.PipelinePrerequisites)
        {
            if (!dependents.ContainsKey(prereq.PrerequisitePipelineId))
                dependents[prereq.PrerequisitePipelineId] = [];
            dependents[prereq.PrerequisitePipelineId].Add(prereq.PipelineId);

            if (!inDegree.ContainsKey(prereq.PipelineId))
                inDegree[prereq.PipelineId] = 0;
            inDegree[prereq.PipelineId]++;
        }

        // Kahn's algorithm: start with all zero-in-degree nodes.
        var queue = new Queue<Guid>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
                queue.Enqueue(kvp.Key);
        }

        var result = new List<Guid>(node.PipelineMemberships.Count);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            if (!dependents.TryGetValue(current, out var deps)) continue;
            foreach (var dep in deps)
            {
                inDegree[dep]--;
                if (inDegree[dep] == 0)
                    queue.Enqueue(dep);
            }
        }

        // Why: fallback to Ordinal order if prerequisites formed a cycle (validator should have caught this).
        if (result.Count < node.PipelineMemberships.Count)
        {
            return node.PipelineMemberships
                .OrderBy(p => p.Ordinal)
                .Select(p => p.PipelineId)
                .ToList();
        }

        return result;
    }
}
