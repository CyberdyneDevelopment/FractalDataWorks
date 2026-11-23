using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>The server stopped taking runs.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(QueueRefusals), "Stopped")]
public sealed class StoppedQueueRefusal : QueueRefusalBase
{
    /// <summary>Initializes a new instance of the <see cref="StoppedQueueRefusal"/> class.</summary>
    public StoppedQueueRefusal()
        : base("Stopped", 150831)
    {
    }

    /// <inheritdoc />
    public override PipelineEnqueueResult Refusal(string reason) => PipelineEnqueueResult.Stopped(reason);
}
