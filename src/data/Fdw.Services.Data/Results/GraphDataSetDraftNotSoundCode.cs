using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set was not saved because the check of it, the one POST /datasets/graph/validate runs, found a problem.
/// </summary>
/// <remarks>
/// Saving a Graph data set judges it as validating does (FDW-5442), so a body that validate calls unsound is refused here, and the message lists each problem with its code
/// and path. An abstract data set, with no components yet, is sound, and an intended source that is not bound yet is information, not a problem. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetDraftNotSound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetDraftNotSoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetDraftNotSoundCode"/> class.</summary>
    public GraphDataSetDraftNotSoundCode()
        : base(21414, "GraphDataSetDraftNotSound",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' is not sound, so it is not saved: {problems}",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller every problem of their own draft, each with its code, message and path, as validating it does.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
