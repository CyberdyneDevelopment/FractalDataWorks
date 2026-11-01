using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A request tried to change credentials that a platform administrator keeps in an external secret manager (FDW-1327).
/// </summary>
/// <remarks>
/// A connection whose credentials an administrator named in an external secret manager is changed only by a platform administrator: nobody else sends values for it, changes which secret it names or clears the reference. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialsManagedExternally", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialsManagedExternallyCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialsManagedExternallyCode"/> class.</summary>
    public CredentialsManagedExternallyCode()
        : base(20011, "CredentialsManagedExternally",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' keeps its credentials in a secret manager a platform administrator named; only a platform administrator may change them",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
