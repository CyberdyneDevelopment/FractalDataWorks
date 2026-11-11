using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A data set whose kind does not page in the store holds more rows than an unpaged read may take (DataGatewayServiceTypes.UnpagedDataSetRowCap) (FDW-4911).
/// </summary>
/// <remarks>
/// Such a kind (an in-memory join) builds its whole set to answer any read, so a stream of it is one unpaged read; the cap is configuration, and a set over it is refused rather than held. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "UnpagedDataSetOverRowCap", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnpagedDataSetOverRowCapCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UnpagedDataSetOverRowCapCode"/> class.</summary>
    public UnpagedDataSetOverRowCapCode()
        : base(20013, "UnpagedDataSetOverRowCap",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' holds {rowCount} rows, more than the {rowCap} an unpaged read of a data set that does not page in the store may take",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
