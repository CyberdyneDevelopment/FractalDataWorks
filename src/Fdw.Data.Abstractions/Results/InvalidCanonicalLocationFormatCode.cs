using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A <see cref="Fdw.Data.DataStores.Abstractions.DataLocation"/> canonical string
/// ("store://path@container[?params]") failed to parse (20001 Validation). ResultDetails
/// carries {Input}/{Reason}.
/// </summary>
[TypeOption(typeof(DataStoresResultCodes), "InvalidCanonicalLocationFormat", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidCanonicalLocationFormatCode : DataStoresResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCanonicalLocationFormatCode"/> class.
    /// </summary>
    public InvalidCanonicalLocationFormatCode()
        : base(
            20001,
            "InvalidCanonicalLocationFormat",
            ResultSeverities.ByName("Error"),
            "Invalid canonical DataLocation string '{Input}': {Reason}",
            isRetryable: false)
    {
    }
}
