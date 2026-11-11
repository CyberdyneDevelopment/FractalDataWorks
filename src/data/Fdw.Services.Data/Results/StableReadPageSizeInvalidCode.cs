using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// The page size a stable read asks for (DataGatewayServiceTypes.StableReadPageSize) is below one (FDW-4911).
/// </summary>
/// <remarks>
/// A page of no rows reads nothing and never ends, so it is refused where the host sets it (registration) and again when a read would use it. HTTP 500: the host's configuration is wrong, not the request.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "StableReadPageSizeInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StableReadPageSizeInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="StableReadPageSizeInvalidCode"/> class.</summary>
    public StableReadPageSizeInvalidCode()
        : base(20009, "StableReadPageSizeInvalid",
            ResultSeverities.ByName("Error"),
            "DataGatewayServiceTypes.StableReadPageSize is {pageSize}; a stable read asks for at least 1 row a page, and the host states the size at registration",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
