using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>A route of the Graph kind was asked about a data set of another kind.</summary>
/// <remarks>Raised by <c>GET /datasets/graph/{name}/graph</c> (FDW-5295): only a Graph data set has a graph to answer. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetNotGraph", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetNotGraphCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetNotGraphCode"/> class.</summary>
    public GraphDataSetNotGraphCode()
        : base(21079, "GraphDataSetNotGraph",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' is of kind '{implementation}', not Graph, so it has no graph to answer",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
