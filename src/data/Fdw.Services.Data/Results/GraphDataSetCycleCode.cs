using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set reads itself through data sets.
/// </summary>
/// <remarks>Reported by the check before save and, in the story that nests data sets, again at read. The message carries the cycle as the chain of data set names. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetCycle", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetCycleCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetCycleCode"/> class.</summary>
    public GraphDataSetCycleCode()
        : base(21407, "GraphDataSetCycle",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' reads itself through data sets: {cycle}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
