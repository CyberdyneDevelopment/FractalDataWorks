using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set reads data sets nested deeper than the stated depth (FDW-5299).
/// </summary>
/// <remarks>
/// A Graph data set reads data sets, which may read data sets in turn. How many levels that may go is configuration, declared with its default on the Graph data set runner's
/// service type and overridable at registration; a graph nested deeper is refused by name before any of it is read, never read in part. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetDepthExceeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetDepthExceededCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetDepthExceededCode"/> class.</summary>
    public GraphDataSetDepthExceededCode()
        : base(21411, "GraphDataSetDepthExceeded",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' reads data sets nested {depth} levels deep, past the {limit} levels a read of a Graph data set may nest, so it is not read",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
