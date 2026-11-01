using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A request named where a connection's credentials are kept and also sent their values (FDW-1327).
/// </summary>
/// <remarks>
/// A connection's credentials are either stored by the platform from the values sent, or kept externally under the secret manager and secrets named; a request does one or the other. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialReferenceAndValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialReferenceAndValueCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialReferenceAndValueCode"/> class.</summary>
    public CredentialReferenceAndValueCode()
        : base(20013, "CredentialReferenceAndValue",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' names where its credentials are kept and also sends their values; send one or the other",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
