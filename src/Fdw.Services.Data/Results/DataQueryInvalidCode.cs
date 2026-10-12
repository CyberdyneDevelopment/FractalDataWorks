using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A data query names a field, operator, direction or paging value the data set cannot honour.
/// </summary>
/// <remarks>
/// Refused rather than silently dropped: a filter that quietly disappears returns MORE rows than the
/// caller asked for, and a caller has no way to notice. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataQueryInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataQueryInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataQueryInvalidCode"/> class.</summary>
    public DataQueryInvalidCode()
        : base(20003, "DataQueryInvalid",
            ResultSeverities.ByName("Error"),
            "Query of data set '{dataSet}' rejected: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
