using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// An intended source of a Graph data set is not stated completely.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>intendedSources/{index}</c>, with the reason the intent's own validator gives (an unknown kind, a missing name, a name the kind does not take). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetIntentInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetIntentInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntentInvalidCode"/> class.</summary>
    public GraphDataSetIntentInvalidCode()
        : base(21410, "GraphDataSetIntentInvalid",
            ResultSeverities.ByName("Error"),
            "Intended source {index} of Graph data set '{dataSet}' is not stated completely: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
