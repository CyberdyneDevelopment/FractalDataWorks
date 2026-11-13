using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>The caller may not read every source a Graph data set reads, so its graph is not answered.</summary>
/// <remarks>
/// Stated in the answer of <c>GET /datasets/graph/{name}/graph</c> beside the fields and the description (FDW-5295), which a caller who may read the data set always
/// has; it is not a failure of the request and names no source. HTTP 400 when it is raised as a result.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetGraphNotReadable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetGraphNotReadableCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetGraphNotReadableCode"/> class.</summary>
    public GraphDataSetGraphNotReadableCode()
        : base(21078, "GraphDataSetGraphNotReadable",
            ResultSeverities.ByName("Warning"),
            "The graph of Graph data set '{dataSet}' is not answered: the caller may not read every source it reads. The description and the fields are",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
