using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Two fields out of a Graph data set have the same name.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>fieldsOut/{field}</c>; names compare without regard to case. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFieldOutDuplicate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFieldOutDuplicateCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldOutDuplicateCode"/> class.</summary>
    public GraphDataSetFieldOutDuplicateCode()
        : base(21402, "GraphDataSetFieldOutDuplicate",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' has more than one field out named '{field}'",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
