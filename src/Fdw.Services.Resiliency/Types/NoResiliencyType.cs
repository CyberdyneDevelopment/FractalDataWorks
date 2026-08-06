using Fdw.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency.Types;

/// <summary>
/// No-op resiliency strategy. Runs the stage once; any failure propagates immediately.
/// Use when a stage explicitly opts out of retry behavior.
/// </summary>
[TypeOption(typeof(ResiliencyTypes), "None")]
public sealed class NoResiliencyType : ResiliencyTypeBase
{
    /// <summary>Initializes a new instance of <see cref="NoResiliencyType"/>.</summary>
    public NoResiliencyType()
        : base(
            id: 1,
            name: "None",
            displayName: "No Resiliency",
            description: "No-op strategy. Stage runs once; failures propagate immediately.")
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Why: NoResiliency runs the stage exactly once with no wrapping.
    /// If the delegate throws or returns failure, that result is returned directly.
    /// </remarks>
    public override Task<IGenericResult> Execute(
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IGenericConfiguration config,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken)
    {
        if (runStage == null) throw new ArgumentNullException(nameof(runStage));
        return runStage(cancellationToken);
    }
}
