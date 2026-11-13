using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A join's key is a field its input does not map.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>components/{component}/settings/{setting}</c>. A join works on the rows its inputs map, so when an input states mappings its key is the target of one of them. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetJoinFieldUnmapped", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetJoinFieldUnmappedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetJoinFieldUnmappedCode"/> class.</summary>
    public GraphDataSetJoinFieldUnmappedCode()
        : base(21408, "GraphDataSetJoinFieldUnmapped",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' joins on '{field}' ({setting}), but its {input} input maps no field of that name: a join key is a field the input maps",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
