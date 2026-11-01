using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A connection's credential field cannot be stored under a secret name every secret manager accepts (FDW-1185).
/// </summary>
/// <remarks>
/// Azure Key Vault, the strictest store, allows a secret name to hold only letters, digits and hyphens, at most 127
/// characters. The name of a stored credential is the connection prefix, a fresh id and the credential field's name, so a
/// field name with any other character, or one long enough to pass the limit, cannot be stored. The field names come from
/// the connection type's own declarations, so this is a declaration to fix, not a request to retry. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialFieldNameRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialFieldNameRefusedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialFieldNameRefusedCode"/> class.</summary>
    public CredentialFieldNameRefusedCode()
        : base(20010, "CredentialFieldNameRefused",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' was not saved: its credential field '{field}' cannot be stored under a secret name (a secret name holds only letters, digits and hyphens, at most {limit} characters)",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller which credential field cannot be stored and why.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
