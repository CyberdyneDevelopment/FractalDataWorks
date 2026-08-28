using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Reads and assembles the rollup execution status tree for a project execution.
/// Walks <see cref="IExecutionTracker.GetChildren"/> recursively to build the full hierarchy.
/// </summary>
public sealed class ProjectExecutionStatusReader : IProjectExecutionStatusReader
{
    private readonly IExecutionTracker _tracker;
    private readonly ILogger<ProjectExecutionStatusReader> _logger;

    private static readonly HashSet<string> TerminalStates = new HashSet<string>(StringComparer.Ordinal)
    {
        "Completed", "Failed", "Cancelled"
    };

    private static readonly HashSet<string> FailureStates = new HashSet<string>(StringComparer.Ordinal)
    {
        "Failed"
    };

    /// <summary>Initializes a new instance of <see cref="ProjectExecutionStatusReader"/>.</summary>
    public ProjectExecutionStatusReader(
        IExecutionTracker tracker,
        ILogger<ProjectExecutionStatusReader>? logger = null)
    {
        _tracker = tracker;
        _logger = logger ?? NullLogger<ProjectExecutionStatusReader>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ProjectExecutionStatusNode>> GetStatusTree(
        Guid projectExecutionItemId,
        CancellationToken cancellationToken = default)
    {
        var itemResult = await _tracker.GetItem(projectExecutionItemId, cancellationToken).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value is null)
        {
            if (itemResult.Messages.Count > 0)
            {
                return itemResult.ToNewResult<ProjectExecutionStatusNode>();
            }

            var notFoundMessage = ProjectOrchestratorLog.ProjectExecutionItemNotFound(
                _logger, projectExecutionItemId);
            return GenericResult<ProjectExecutionStatusNode>.Failure(notFoundMessage);
        }

        var rootNode = await BuildNode(itemResult.Value, depth: 0, cancellationToken).ConfigureAwait(false);
        return GenericResult<ProjectExecutionStatusNode>.Success(rootNode);
    }

    private async Task<ProjectExecutionStatusNode> BuildNode(
        IExecutionItem item,
        int depth,
        CancellationToken cancellationToken)
    {
        var node = new ProjectExecutionStatusNode
        {
            ExecutionItem = item,
            Depth = depth,
            RollupState = item.State?.Name ?? "Unknown"
        };

        var childrenResult = await _tracker.GetChildren(item.Id, cancellationToken).ConfigureAwait(false);
        if (!childrenResult.IsSuccess || childrenResult.Value is null)
        {
            return node;
        }

        foreach (var child in childrenResult.Value)
        {
            var childNode = await BuildNode(child, depth + 1, cancellationToken).ConfigureAwait(false);
            node.Children.Add(childNode);
        }

        if (node.Children.Count > 0)
        {
            node.RollupState = ComputeRollupState(node.Children);
        }

        return node;
    }

    /// <summary>
    /// Computes the rollup state for a parent node from its children's rollup states.
    /// Severity hierarchy: Failed > Running > Completed > Cancelled > Unknown.
    /// </summary>
    private static string ComputeRollupState(IList<ProjectExecutionStatusNode> children)
    {
        var hasRunning = false;
        var hasFailed = false;
        var allTerminal = true;

        foreach (var child in children)
        {
            var state = child.RollupState;
            if (FailureStates.Contains(state))
            {
                hasFailed = true;
            }
            else if (string.Equals(state, "Running", StringComparison.Ordinal))
            {
                hasRunning = true;
            }

            if (!TerminalStates.Contains(state))
            {
                allTerminal = false;
            }
        }

        if (hasFailed)
        {
            return "Failed";
        }

        if (hasRunning)
        {
            return "Running";
        }

        if (allTerminal)
        {
            return "Completed";
        }

        return "Initialized";
    }
}
