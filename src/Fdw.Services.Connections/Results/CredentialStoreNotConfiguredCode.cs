using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// The secret manager connection credentials are stored in is not configured (FDW-1086).
/// </summary>
/// <remarks>
/// The server setting names it; nothing guesses one. Configuration, so the caller sees only that the request could not be completed.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialStoreNotConfigured", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialStoreNotConfiguredCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialStoreNotConfiguredCode"/> class.</summary>
    public CredentialStoreNotConfiguredCode()
        : base(60001, "CredentialStoreNotConfigured",
            ResultSeverities.ByName("Error"),
            "Connection credentials cannot be stored: the server setting '{setting}' names no secret manager",
            isRetryable: false)
    {
    }
}
