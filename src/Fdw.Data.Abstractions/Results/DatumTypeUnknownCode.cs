using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A request named a <c>DatumTypeId</c> that none of the <c>DatumTypes</c> options declares.
/// </summary>
/// <remarks>
/// The meaning is the catalog's OutOfRange (RESULTCODE-CATALOG.md): the id names no type in the defined
/// set. 20003 already belongs to DataClassificationUnknown in this collection, and a collection's ids are
/// unique, so this takes 21002 in category 2's package band. HTTP 400. It is a refusal, not "not typed",
/// because a caller who names a type means one (FDW-988).
/// </remarks>
[TypeOption(typeof(DataStoresResultCodes), "DatumTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatumTypeUnknownCode : DataStoresResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatumTypeUnknownCode"/> class.
    /// </summary>
    public DatumTypeUnknownCode()
        : base(21002, "DatumTypeUnknown", ResultSeverities.ByName("Error"),
            "Datum type '{DatumTypeId}' is not one of the datum types",
            isRetryable: false)
    {
    }
}
