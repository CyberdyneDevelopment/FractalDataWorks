using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A request tried to choose which secret manager or which secret a connection reads (FDW-1086).
/// </summary>
/// <remarks>
/// A connection's credentials are written through the connection and stored where the server chooses; a caller who could name the secret could make the connection read any secret the platform holds. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialReferenceReserved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialReferenceReservedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialReferenceReservedCode"/> class.</summary>
    public CredentialReferenceReservedCode()
        : base(20005, "CredentialReferenceReserved",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' sets '{property}', which names where its credentials are stored; the server chooses that. Send the credential's value in 'credentials' instead",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
