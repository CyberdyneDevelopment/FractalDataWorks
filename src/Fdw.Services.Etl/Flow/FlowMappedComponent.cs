using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A component whose inputs map their rows as they arrive (FDW-2013): the component works on the mapped rows and never sees the rows as they came.
/// </summary>
/// <remarks>
/// <para>
/// Why a wrapper and not a mapping in each component: a mapping is not a step of its own, it is what happens to rows at an input, for every kind of
/// component alike. The wrapped component keeps its nodes, its error outputs and its work; only the rows it reads from a mapped input differ. An
/// input with no mapping is read as it is.
/// </para>
/// <para>
/// A mapped input shows exactly the mapped fields. A row that cannot be mapped (a mapped field it lacks, a required value that is null, a
/// transformation or a conversion that fails) fails the run, naming the component, the input and the field; no value is invented for it.
/// </para>
/// </remarks>
public sealed class FlowMappedComponent : IFlowComponent, IFlowErrorOutputs
{
    private readonly IFlowComponent _inner;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<FlowFieldMap>> _mappings;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowMappedComponent"/> class.</summary>
    /// <param name="inner">The component that works on the mapped rows.</param>
    /// <param name="mappings">The mapping of each mapped input, by input node name; at least one input, each with at least one field and no target twice.</param>
    /// <param name="logger">The logger.</param>
    public FlowMappedComponent(IFlowComponent inner, IReadOnlyDictionary<string, IReadOnlyList<FlowFieldMap>> mappings, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(mappings);
        if (mappings.Count == 0)
        {
            throw new ArgumentException("A mapped component maps at least one input.", nameof(mappings));
        }

        foreach (var (input, maps) in mappings)
        {
            if (!inner.Inputs.Contains(input, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The component '{inner.Name}' has no input node '{input}' to map.", nameof(mappings));
            }

            if (maps.Count == 0)
            {
                throw new ArgumentException($"The mapping of input '{input}' states at least one output field.", nameof(mappings));
            }

            if (maps.Select(map => map.Target).Distinct(StringComparer.OrdinalIgnoreCase).Count() != maps.Count)
            {
                throw new ArgumentException($"The mapping of input '{input}' states each output field once.", nameof(mappings));
            }
        }

        _inner = inner;
        _mappings = new Dictionary<string, IReadOnlyList<FlowFieldMap>>(mappings, StringComparer.OrdinalIgnoreCase);
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => _inner.Inputs;

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => _inner.Outputs;

    /// <inheritdoc />
    public IReadOnlyList<string> ErrorOutputs => _inner is IFlowErrorOutputs errors ? errors.ErrorOutputs : [];

    /// <inheritdoc />
    public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default) =>
        _inner.Run(new MappedPorts(ports, _mappings, Name, _logger), cancellationToken);

    private sealed class MappedPorts(IFlowPorts ports, IReadOnlyDictionary<string, IReadOnlyList<FlowFieldMap>> mappings, string component, ILogger logger) : IFlowPorts
    {
        public IFlowInput Input(string name) =>
            mappings.TryGetValue(name, out var maps) ? new MappedInput(ports.Input(name), maps, component, logger) : ports.Input(name);

        public IFlowOutput Output(string name) => ports.Output(name);
    }

    private sealed class MappedInput(IFlowInput input, IReadOnlyList<FlowFieldMap> maps, string component, ILogger logger) : IFlowInput
    {
        public string Name => input.Name;

        public async Task<FlowBatch?> Read(CancellationToken cancellationToken = default)
        {
            if (await input.Read(cancellationToken).ConfigureAwait(false) is not { } batch)
            {
                return null;
            }

            var mapped = await FlowRowMapper.Apply(maps, batch, component, Name, logger, cancellationToken).ConfigureAwait(false);
            return mapped.IsFailure
                ? throw new FlowMappingRefusedException(mapped.CurrentMessage)
                : mapped.Value;
        }
    }
}
