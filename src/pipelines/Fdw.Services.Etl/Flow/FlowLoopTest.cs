using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The decisions a loop's test depends on, built as a level of their own (FDW-6134): a loop that tests before its body runs them first, in a scope of variables that ends with
/// the test, and reads the output it is told to take as its test. A decision judges variables and moves nothing, so running it ahead of the body has no effect of its own.
/// </summary>
/// <remarks>It is handed to a loop's builder after the body, so a loop builder tells the two apart by this type.</remarks>
public sealed class FlowLoopTest : IFlowStep
{
    private readonly IFlowStep _decisions;

    /// <summary>Initializes a new instance of the <see cref="FlowLoopTest"/> class.</summary>
    /// <param name="decisions">The level that holds the decisions the test depends on.</param>
    public FlowLoopTest(IFlowStep decisions)
    {
        _decisions = decisions;
    }

    /// <inheritdoc />
    public string Name => _decisions.Name;

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default) =>
        _decisions.Run(context, cancellationToken);
}
