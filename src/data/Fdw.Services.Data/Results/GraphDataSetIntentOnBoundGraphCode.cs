using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set is saved with components and with intended sources, which only an abstract one has.
/// </summary>
/// <remarks>Refused when the graph is saved (FDW-5287). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetIntentOnBoundGraph", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetIntentOnBoundGraphCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntentOnBoundGraphCode"/> class.</summary>
    public GraphDataSetIntentOnBoundGraphCode()
        : base(21034, "GraphDataSetIntentOnBoundGraph",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' has components, so it is bound and states no intended sources: send the components and an empty list of intended sources to bind it",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
