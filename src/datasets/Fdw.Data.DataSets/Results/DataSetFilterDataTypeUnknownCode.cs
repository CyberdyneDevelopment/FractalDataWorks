using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set's own filter condition names a data type that is not a DataTypes name (FDW-4997), so its value cannot be read. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetFilterDataTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFilterDataTypeUnknownCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetFilterDataTypeUnknownCode"/> class.
    /// </summary>
    public DataSetFilterDataTypeUnknownCode()
        : base(20024, "DataSetFilterDataTypeUnknown",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' filter {ordinal} names data type '{dataType}', which is not a data type; use one of: {dataTypes}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
