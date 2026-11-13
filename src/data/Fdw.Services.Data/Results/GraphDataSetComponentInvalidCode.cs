using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// The engine's own check of a graph finds a problem in a Graph data set's components or links.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290) with the path and reason the graph pipeline's check gives for the same problem (a link end, a flow shape, a component's settings): one code, because the reason is the engine's. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetComponentInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetComponentInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentInvalidCode"/> class.</summary>
    public GraphDataSetComponentInvalidCode()
        : base(21409, "GraphDataSetComponentInvalid",
            ResultSeverities.ByName("Error"),
            "The graph of Graph data set '{dataSet}' is not sound: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
