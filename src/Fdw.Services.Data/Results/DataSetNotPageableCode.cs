using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A data set cannot yet answer a paged query: its kind does not apply ordering and paging in the
/// store, or it aggregates.
/// </summary>
/// <remarks>
/// Refused rather than served, because serving it pages wrongly: the whole set is read, the page is
/// cut from its front, and the next page returns the same rows — a client paging to the end never
/// gets there. Compound joins, Federated in-memory joins and aggregated Simple sets are the cases
/// today. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataSetNotPageable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetNotPageableCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetNotPageableCode"/> class.</summary>
    public DataSetNotPageableCode()
        : base(20004, "DataSetNotPageable",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' cannot be queried page by page yet: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the data set cannot be read this way: the message names their own data set and the kind's limit.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
