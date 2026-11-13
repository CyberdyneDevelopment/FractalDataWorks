using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A read of a Graph data set ended because its graph ran longer than the stated limit (FDW-5291).
/// </summary>
/// <remarks>
/// A read of a Graph data set ended because its graph ran longer than the stated limit (FDW-5291). The rows read so far are not returned. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetTimeLimitExceeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetTimeLimitExceededCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetTimeLimitExceededCode"/> class.</summary>
    public GraphDataSetTimeLimitExceededCode()
        : base(21072, "GraphDataSetTimeLimitExceeded",
            ResultSeverities.ByName("Error"),
            "The graph of data set '{dataSet}' did not finish within the {limit} a read of it may take, so no rows are returned",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which data set's read ran past the time limit and what that limit is.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
