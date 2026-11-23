using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>A container (FDW-6134): runs the components inside it once, in a scope of variables of its own that sees the variables of the level around it.</summary>
public sealed class FlowContainer : IFlowStep
{
    private readonly IReadOnlyList<IFlowStep> _body;

    /// <summary>Initializes a new instance of the <see cref="FlowContainer"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="body">The steps it runs; at least one.</param>
    public FlowContainer(string name, IReadOnlyList<IFlowStep> body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (body.Count == 0)
        {
            throw new ArgumentException("A container's body has at least one step.", nameof(body));
        }

        Name = name;
        _body = body;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default) =>
        FlowSequence.Run(_body, context.CreateChild(), cancellationToken);
}
