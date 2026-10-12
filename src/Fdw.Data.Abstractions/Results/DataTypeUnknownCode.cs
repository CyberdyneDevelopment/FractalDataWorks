using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A request named a portable data type that none of the <c>DataTypes</c> options declares, or named none
/// where one is required.
/// </summary>
/// <remarks>
/// The meaning is the catalog's OutOfRange (RESULTCODE-CATALOG.md): the name is not in the defined set. 21002
/// is DatumTypeUnknown and 21003 DataValueNotConvertible in this collection, so this takes 21004 in the same
/// band. HTTP 400, and the caller is
/// told which name and which options, because naming a valid one is the only fix (FDW-1103).
/// </remarks>
[TypeOption(typeof(DataStoresResultCodes), "DataTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataTypeUnknownCode : DataStoresResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTypeUnknownCode"/> class.
    /// </summary>
    public DataTypeUnknownCode()
        : base(21004, "DataTypeUnknown", ResultSeverities.ByName("Error"),
            "Data type '{DataType}' is not one of the data types ({Options})",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
