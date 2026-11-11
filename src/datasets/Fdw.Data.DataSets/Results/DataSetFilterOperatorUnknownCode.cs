using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set's own filter condition names an operator that is not a FilterOperators name (FDW-4997); no alias stands in for one. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetFilterOperatorUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFilterOperatorUnknownCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetFilterOperatorUnknownCode"/> class.
    /// </summary>
    public DataSetFilterOperatorUnknownCode()
        : base(20023, "DataSetFilterOperatorUnknown",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' filter {ordinal} names operator '{operator}', which is not an operator; use one of: {operators}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
