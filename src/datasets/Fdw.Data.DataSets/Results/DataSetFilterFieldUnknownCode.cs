using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set's own filter condition names a field that is not one of its stored, non-calculated fields (FDW-4997): the store has no column for it. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetFilterFieldUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFilterFieldUnknownCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetFilterFieldUnknownCode"/> class.
    /// </summary>
    public DataSetFilterFieldUnknownCode()
        : base(20022, "DataSetFilterFieldUnknown",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' filter {ordinal} names field '{field}', which is not a stored field of the data set; name one of its fields that is not calculated",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
