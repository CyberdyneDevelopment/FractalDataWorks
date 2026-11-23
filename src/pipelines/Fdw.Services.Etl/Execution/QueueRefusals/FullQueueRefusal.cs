using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>The queue is full.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(QueueRefusals), "Full")]
public sealed class FullQueueRefusal : QueueRefusalBase
{
    /// <summary>Initializes a new instance of the <see cref="FullQueueRefusal"/> class.</summary>
    public FullQueueRefusal()
        : base("Full", 150830)
    {
    }

    /// <inheritdoc />
    public override PipelineEnqueueResult Refusal(string reason) => PipelineEnqueueResult.Full(reason);
}
