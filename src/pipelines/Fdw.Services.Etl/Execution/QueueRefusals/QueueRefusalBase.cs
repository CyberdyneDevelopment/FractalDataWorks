using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>Base class for the refusals the run queue gives.</summary>
public abstract class QueueRefusalBase : TypeOptionBase<QueueRefusalBase>, IQueueRefusal
{
    /// <summary>Initializes a new instance of the <see cref="QueueRefusalBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="number">The number of the failure message that states this refusal.</param>
    protected QueueRefusalBase(string name, int number)
        : base(name)
    {
        Number = number;
    }

    /// <inheritdoc />
    [TypeLookup("ByNumber")]
    public int Number { get; }

    /// <inheritdoc />
    public abstract PipelineEnqueueResult Refusal(string reason);
}
