using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A component that is built when its stage starts, from the variables as the components of lower stages left them (FDW-1958, FDW-2041): how a
/// source reads a file whose name an earlier component calculated. Its nodes are known up front, from its kind; the component it builds is
/// run in its place, and a build that fails fails the run with the reason.
/// </summary>
public sealed class FlowLazyComponent : IFlowComponent, IFlowErrorOutputs, IFlowStepBound
{
    private readonly Func<FlowVariables, IGenericResult<IFlowComponent>> _build;
    private FlowStepContext? _context;

    /// <summary>Initializes a new instance of the <see cref="FlowLazyComponent"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="inputs">The names of its input nodes.</param>
    /// <param name="outputs">The names of its output nodes.</param>
    /// <param name="errorOutputs">The names of the output nodes that carry rejected rows.</param>
    /// <param name="build">Builds the component to run from the variables in scope when its stage starts.</param>
    public FlowLazyComponent(
        string name,
        IReadOnlyList<string> inputs,
        IReadOnlyList<string> outputs,
        IReadOnlyList<string> errorOutputs,
        Func<FlowVariables, IGenericResult<IFlowComponent>> build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
        ErrorOutputs = errorOutputs ?? throw new ArgumentNullException(nameof(errorOutputs));
        _build = build ?? throw new ArgumentNullException(nameof(build));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ErrorOutputs { get; }

    /// <inheritdoc />
    void IFlowStepBound.Bind(FlowStepContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var context = _context ?? throw new InvalidOperationException($"The component '{Name}' was run before the step it belongs to gave it a context.");
        var built = _build(context.Variables);
        if (built.IsFailure || built.Value is null)
        {
            return built;
        }

        return await built.Value.Run(ports, cancellationToken).ConfigureAwait(false);
    }
}
