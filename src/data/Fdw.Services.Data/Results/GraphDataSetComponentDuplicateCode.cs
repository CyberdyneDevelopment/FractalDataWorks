using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Two components of a Graph data set's graph have the same name.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>components/{component}</c>; a link names a component by its name, so names are unique in a graph. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetComponentDuplicate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetComponentDuplicateCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentDuplicateCode"/> class.</summary>
    public GraphDataSetComponentDuplicateCode()
        : base(21403, "GraphDataSetComponentDuplicate",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' has more than one component named '{component}'",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
