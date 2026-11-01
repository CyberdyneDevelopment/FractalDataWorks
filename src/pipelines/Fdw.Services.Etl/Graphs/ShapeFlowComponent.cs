using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// A component that has only a name and nodes (FDW-1396): what the engine's graph check needs to judge the shape of a stored
/// graph without a data gateway or a load executor. It never runs.
/// </summary>
public sealed class ShapeFlowComponent : IFlowComponent
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ShapeFlowComponent"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="inputs">The input node names.</param>
    /// <param name="outputs">The output node names.</param>
    /// <param name="logger">The logger a refusal to run is written to.</param>
    public ShapeFlowComponent(string name, IReadOnlyList<string> inputs, IReadOnlyList<string> outputs, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; }

    /// <inheritdoc />
    public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult>(GenericResult.Failure(GraphLog.ShapeCannotRun(_logger, Name)));
}
