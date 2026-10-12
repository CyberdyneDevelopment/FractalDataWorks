using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A request named a data store the host declared for the platform's own use, and the caller is not the system
/// context (FDW-1068).
/// </summary>
/// <remarks>
/// The stores the host declares in configurationSchema.json hold the platform's own configuration, credentials and
/// sessions. The platform reads them through their configuration gateway; no request reaches them through the main
/// data gateway, whether it names the store directly, a container in it, or a data set over it. The reason names the
/// store so the caller knows what was refused. A validation code, because only those carry their reason to the caller
/// (FDW-1046). HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "HostDeclaredStoreRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HostDeclaredStoreRefusedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="HostDeclaredStoreRefusedCode"/> class.</summary>
    public HostDeclaredStoreRefusedCode()
        : base(20007, "HostDeclaredStoreRefused",
            ResultSeverities.ByName("Error"),
            "Data store '{dataStore}' is the platform's own and is not reachable from a request",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
