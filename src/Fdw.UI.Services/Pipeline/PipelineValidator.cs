using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Conventions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.UI.Pipelines.Clients.Models;

namespace Fdw.UI.Services.Pipeline;

/// <summary>
/// Validates pipeline definitions.
/// </summary>
public sealed class PipelineValidator : IPipelineValidator
{
    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Pipeline validation logic — independent checks for name, tasks, connections, cycles, duplicates
    public IGenericResult ValidatePipeline(PipelineEditModel pipeline)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(pipeline.Name))
        {
            errors.Add("Pipeline name is required.");
        }
        else if (pipeline.Name.Length > 128)
        {
            errors.Add("Pipeline name must be 128 characters or less.");
        }

        if (pipeline.Tasks.Count == 0)
        {
            errors.Add("Pipeline must have at least one task.");
        }

        foreach (var task in pipeline.Tasks)
        {
            var taskErrors = ValidateTask(task);
            errors.AddRange(taskErrors);
        }

        foreach (var connection in pipeline.Connections)
        {
            var sourceTask = pipeline.Tasks.FirstOrDefault(t => t.Id == connection.SourceTaskId);
            var targetTask = pipeline.Tasks.FirstOrDefault(t => t.Id == connection.TargetTaskId);

            if (sourceTask == null)
            {
                errors.Add($"Connection references non-existent source task {connection.SourceTaskId}.");
            }

            if (targetTask == null)
            {
                errors.Add($"Connection references non-existent target task {connection.TargetTaskId}.");
            }

            if (connection.SourceTaskId == connection.TargetTaskId)
            {
                errors.Add("A task cannot connect to itself.");
            }
        }

        if (HasCycle(pipeline))
        {
            errors.Add("Pipeline contains a circular dependency.");
        }

        var duplicateNames = pipeline.Tasks
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var name in duplicateNames)
        {
            errors.Add($"Duplicate task name: '{name}'.");
        }

        if (errors.Count > 0)
        {
            // Why: consolidated validation errors carried as an ErrorMessage (IGenericMessage) —
            // the Failure(string) overload is gone; ErrorMessage avoids FDW004 (manual GenericMessage).
            return GenericResult.Failure(new ErrorMessage(string.Join(" ", errors)));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public bool WouldCreateCycle(PipelineEditModel pipeline, Guid sourceId, Guid targetId)
    {
        return HasPath(pipeline, targetId, sourceId);
    }

    private static List<string> ValidateTask(TaskEditModel task)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(task.TaskType))
        {
            errors.Add($"Task '{task.Name}' is missing a task type.");
        }

        if (string.IsNullOrWhiteSpace(task.Name))
        {
            errors.Add("Task name is required.");
        }
        else if (task.Name.Length > 128)
        {
            errors.Add($"Task name '{task.Name}' must be 128 characters or less.");
        }

        ValidateTaskTypeConfiguration(task, errors);

        return errors;
    }

    [ConventionOverride(MaxCyclomaticComplexity = 25)]  // Task-type dispatch — necessary branching for per-type config validation (SqlQuery, Filter, Map, Join)
    private static void ValidateTaskTypeConfiguration(TaskEditModel task, List<string> errors)
    {
        if (string.Equals(task.TaskType, "SqlQuery", StringComparison.OrdinalIgnoreCase))
        {
            if (!task.Configuration.TryGetValue("Query", out var query) ||
                string.IsNullOrWhiteSpace(query?.ToString()))
            {
                errors.Add($"Task '{task.Name}' (SqlQuery) requires a 'Query' configuration value.");
            }
        }
        else if (string.Equals(task.TaskType, "Filter", StringComparison.OrdinalIgnoreCase))
        {
            if (!task.Configuration.TryGetValue("Expression", out var expression) ||
                string.IsNullOrWhiteSpace(expression?.ToString()))
            {
                errors.Add($"Task '{task.Name}' (Filter) requires an 'Expression' configuration value.");
            }
        }
        else if (string.Equals(task.TaskType, "Map", StringComparison.OrdinalIgnoreCase))
        {
            if (!task.Configuration.TryGetValue("Mappings", out var mappings) || mappings is null)
            {
                errors.Add($"Task '{task.Name}' (Map) requires a 'Mappings' configuration value.");
            }
        }
        else if (string.Equals(task.TaskType, "Join", StringComparison.OrdinalIgnoreCase))
        {
            if (!task.Configuration.TryGetValue("JoinType", out var joinType) ||
                string.IsNullOrWhiteSpace(joinType?.ToString()))
            {
                errors.Add($"Task '{task.Name}' (Join) requires a 'JoinType' configuration value.");
            }

            if (!task.Configuration.TryGetValue("JoinCondition", out var joinCondition) ||
                string.IsNullOrWhiteSpace(joinCondition?.ToString()))
            {
                errors.Add($"Task '{task.Name}' (Join) requires a 'JoinCondition' configuration value.");
            }
        }
    }

    private static bool HasCycle(PipelineEditModel pipeline)
    {
        var taskIds = new HashSet<Guid>(pipeline.Tasks.Select(t => t.Id));
        var adjacencyList = new Dictionary<Guid, IList<Guid>>();

        foreach (var taskId in taskIds)
        {
            adjacencyList[taskId] = new List<Guid>();
        }

        foreach (var connection in pipeline.Connections)
        {
            if (adjacencyList.TryGetValue(connection.SourceTaskId, out var neighbors))
            {
                neighbors.Add(connection.TargetTaskId);
            }
        }

        var visited = new HashSet<Guid>();
        var recursionStack = new HashSet<Guid>();

        foreach (var taskId in taskIds)
        {
            if (DetectCycleDfs(taskId, adjacencyList, visited, recursionStack))
            {
                return true;
            }
        }

        return false;
    }

    private static bool DetectCycleDfs(
        Guid taskId,
        IDictionary<Guid, IList<Guid>> adjacencyList,
        HashSet<Guid> visited,
        HashSet<Guid> recursionStack)
    {
        if (recursionStack.Contains(taskId))
        {
            return true;
        }

        if (visited.Contains(taskId))
        {
            return false;
        }

        visited.Add(taskId);
        recursionStack.Add(taskId);

        if (adjacencyList.TryGetValue(taskId, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (DetectCycleDfs(neighbor, adjacencyList, visited, recursionStack))
                {
                    return true;
                }
            }
        }

        recursionStack.Remove(taskId);
        return false;
    }

    private static bool HasPath(PipelineEditModel pipeline, Guid sourceId, Guid targetId)
    {
        var adjacencyList = new Dictionary<Guid, IList<Guid>>();

        foreach (var task in pipeline.Tasks)
        {
            adjacencyList[task.Id] = new List<Guid>();
        }

        foreach (var connection in pipeline.Connections)
        {
            if (adjacencyList.TryGetValue(connection.SourceTaskId, out var neighbors))
            {
                neighbors.Add(connection.TargetTaskId);
            }
        }

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(sourceId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == targetId)
            {
                return true;
            }

            if (visited.Contains(current))
            {
                continue;
            }

            visited.Add(current);

            if (adjacencyList.TryGetValue(current, out var currentNeighbors))
            {
                foreach (var neighbor in currentNeighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return false;
    }
}
