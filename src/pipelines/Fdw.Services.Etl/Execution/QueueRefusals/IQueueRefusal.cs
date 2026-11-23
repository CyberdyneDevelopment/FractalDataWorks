using Fdw.Collections;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>One of the refusals the run queue gives, found by the number of the message that says it.</summary>
public interface IQueueRefusal : ITypeOption<int, QueueRefusalBase>
{
    /// <summary>Gets the number of the failure message that states this refusal (its EventId in <c>PipelineRunStartLog</c>).</summary>
    int Number { get; }

    /// <summary>Makes the queue's answer for this refusal.</summary>
    /// <param name="reason">The reason the failure message gives.</param>
    /// <returns>The refusal, with its code and the reason.</returns>
    PipelineEnqueueResult Refusal(string reason);
}
