using Fdw.Operations.Abstractions.Execution;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>Reads an execution item as the detail an endpoint answers with (FDW-1740).</summary>
internal static class ExecutionDetails
{
    /// <summary>Maps an execution item to its detail.</summary>
    /// <param name="item">The execution item.</param>
    /// <returns>The detail.</returns>
    internal static ExecutionDetailDto Of(IExecutionItem item) => new()
    {
        Id = item.Id,
        ParentId = item.ParentId,
        RootId = item.RootId,
        ItemType = item.ItemType.Name,
        Name = item.Name,
        State = item.State.Name,
        CorrelationId = item.CorrelationId,
        TriggerSource = item.TriggerSource,
        Parameters = item.Parameters,
        CreatedAt = item.CreatedAt,
        StartedAt = item.StartedAt,
        CompletedAt = item.CompletedAt,
        ResultCode = item.ResultCode,
        ResultMessage = item.ResultMessage,
    };
}
