using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// The secret manager that holds connection credentials refused or failed to store one (FDW-1176).
/// </summary>
/// <remarks>
/// The request was fine and the failure is the credential store's, so this is a dependency failure, not caller
/// validation. The message names the connection, the credential field and the store's own result code, and nothing the
/// store said beyond that code, so an operator can find the cause in the log without the caller seeing store internals.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialPersistFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialPersistFailedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialPersistFailedCode"/> class.</summary>
    public CredentialPersistFailedCode()
        : base(70002, "CredentialPersistFailed",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' was not saved: its credential '{field}' could not be stored (credential store result {reason})",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller which credential could not be stored and the store's result code.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
