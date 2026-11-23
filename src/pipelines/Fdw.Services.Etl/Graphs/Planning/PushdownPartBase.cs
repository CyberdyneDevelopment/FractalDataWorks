using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>Base class for the options of <see cref="PushdownParts"/>, using the CRTP pattern (FDW-6969).</summary>
public abstract class PushdownPartBase : TypeOptionBase<PushdownPartBase>, IPushdownPart
{
    /// <summary>Initializes a new instance of the <see cref="PushdownPartBase"/> class.</summary>
    /// <param name="name">The component type this option pushes.</param>
    /// <param name="combines">Whether the component combines inputs.</param>
    /// <param name="outputName">The output the component sends its rows out by.</param>
    /// <param name="buildSideInput">The input it holds whole while it streams its other input, or empty when none.</param>
    protected PushdownPartBase(string name, bool combines, string outputName, string buildSideInput) : base(name)
    {
        Combines = combines;
        OutputName = outputName;
        BuildSideInput = buildSideInput;
    }

    /// <inheritdoc />
    public bool Combines { get; }

    /// <inheritdoc />
    public string OutputName { get; }

    /// <inheritdoc />
    public string BuildSideInput { get; }

    /// <inheritdoc />
    /// <remarks>Empty here: only a read states a store, and a read's part says which.</remarks>
    public virtual Task<string> ReadStore(PushdownScope scope, PipelineComponentConfiguration component, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Empty);

    /// <inheritdoc />
    public abstract Task<PushdownAttempt> Resolve(PushdownScope scope, PipelineComponentConfiguration component, bool top, CancellationToken cancellationToken = default);
}
