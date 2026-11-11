using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A stable read was asked with a command that is not a query (FDW-4911).
/// </summary>
/// <remarks>
/// A cursor or a keyset page is a read; any other command is refused rather than run. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "StableReadNeedsQuery", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StableReadNeedsQueryCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="StableReadNeedsQueryCode"/> class.</summary>
    public StableReadNeedsQueryCode()
        : base(20012, "StableReadNeedsQuery",
            ResultSeverities.ByName("Error"),
            "A stable read of '{target}' takes a query command; '{commandType}' is not one",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
