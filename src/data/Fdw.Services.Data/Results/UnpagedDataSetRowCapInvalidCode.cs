using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// The most rows an unpaged data set read may take (DataGatewayServiceTypes.UnpagedDataSetRowCap) is below one (FDW-4911).
/// </summary>
/// <remarks>
/// A cap of no rows refuses every such read, so it is refused where the host sets it (registration) and again when a read would use it. HTTP 500.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "UnpagedDataSetRowCapInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnpagedDataSetRowCapInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UnpagedDataSetRowCapInvalidCode"/> class.</summary>
    public UnpagedDataSetRowCapInvalidCode()
        : base(20014, "UnpagedDataSetRowCapInvalid",
            ResultSeverities.ByName("Error"),
            "DataGatewayServiceTypes.UnpagedDataSetRowCap is {rowCap}; an unpaged data set read takes at least 1 row, and the host states the cap at registration",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
