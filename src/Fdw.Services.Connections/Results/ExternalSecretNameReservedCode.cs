using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A request named a secret in the form the platform mints for its own credentials (FDW-1327).
/// </summary>
/// <remarks>
/// The platform deletes the secrets it minted when a connection stops naming them, so a name in that form may not be chosen by anyone: it could point at another connection's secret. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "ExternalSecretNameReserved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExternalSecretNameReservedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ExternalSecretNameReservedCode"/> class.</summary>
    public ExternalSecretNameReservedCode()
        : base(20014, "ExternalSecretNameReserved",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' names secret '{secret}', which has the form of a name the platform generates for its own credentials; name the secret as your secret manager keeps it",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
