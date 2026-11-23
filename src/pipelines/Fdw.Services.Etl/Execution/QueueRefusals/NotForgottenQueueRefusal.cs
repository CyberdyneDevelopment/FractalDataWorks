using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>The queue refused the run and could not record the refusal.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(QueueRefusals), "NotForgotten")]
public sealed class NotForgottenQueueRefusal : QueueRefusalBase
{
    /// <summary>Initializes a new instance of the <see cref="NotForgottenQueueRefusal"/> class.</summary>
    public NotForgottenQueueRefusal()
        : base("NotForgotten", 90830)
    {
    }

    /// <inheritdoc />
    public override PipelineEnqueueResult Refusal(string reason) => PipelineEnqueueResult.NotForgotten(reason);
}
