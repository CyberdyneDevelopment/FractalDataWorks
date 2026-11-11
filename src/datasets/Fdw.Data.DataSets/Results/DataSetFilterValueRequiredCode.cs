using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set's own filter condition uses an operator that compares with a value and states none (FDW-4997). Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetFilterValueRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFilterValueRequiredCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetFilterValueRequiredCode"/> class.
    /// </summary>
    public DataSetFilterValueRequiredCode()
        : base(20025, "DataSetFilterValueRequired",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' filter {ordinal} on field '{field}' uses operator '{operator}', which compares with a value, and states none",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
