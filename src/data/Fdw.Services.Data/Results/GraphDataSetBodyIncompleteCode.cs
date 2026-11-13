using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>An update of a Graph data set sends the components of its graph and not its links, or the links and not the components.</summary>
/// <remarks>Refused (FDW-5287): a link names components, so the graph is replaced whole or not at all. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetBodyIncomplete", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetBodyIncompleteCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetBodyIncompleteCode"/> class.</summary>
    public GraphDataSetBodyIncompleteCode()
        : base(21047, "GraphDataSetBodyIncomplete",
            ResultSeverities.ByName("Error"),
            "The update of Graph data set '{dataSet}' sends only the {sent} of its graph: send the components and the links together",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which part of the graph their update sent alone.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
