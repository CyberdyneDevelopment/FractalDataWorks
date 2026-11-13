using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// An intended source names a data set that does not exist.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>intendedSources/{index}</c>, because saving refuses it: an intended data set is one that exists, abstract or not. An intended data store need not exist (it may be external) and is never reported. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetIntentSourceMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetIntentSourceMissingCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntentSourceMissingCode"/> class.</summary>
    public GraphDataSetIntentSourceMissingCode()
        : base(21404, "GraphDataSetIntentSourceMissing",
            ResultSeverities.ByName("Error"),
            "Intended source {index} of Graph data set '{dataSet}' names data set '{source}', which does not exist: an intended data set is one that exists, abstract or not",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
