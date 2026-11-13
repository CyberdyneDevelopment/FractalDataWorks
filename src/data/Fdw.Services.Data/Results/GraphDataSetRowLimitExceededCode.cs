using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A read of a Graph data set ended because its graph produced more rows than the stated limit (FDW-5291).
/// </summary>
/// <remarks>
/// A read of a Graph data set ended because its graph produced more rows than the stated limit (FDW-5291). The rows read so far are not returned, so a partial result is never presented as the whole. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetRowLimitExceeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetRowLimitExceededCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetRowLimitExceededCode"/> class.</summary>
    public GraphDataSetRowLimitExceededCode()
        : base(21071, "GraphDataSetRowLimitExceeded",
            ResultSeverities.ByName("Error"),
            "The graph of data set '{dataSet}' produced more than the {limit} rows a read of it may return, so no rows are returned",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which data set's read passed the row limit and what that limit is.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
