using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// Testing an unsaved connection cannot use credentials (FDW-1086).
/// </summary>
/// <remarks>
/// Credentials are only ever stored through a saved connection, so an unsaved one has none to test with; save the connection with its credentials and test it then. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialTestNotSupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialTestNotSupportedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialTestNotSupportedCode"/> class.</summary>
    public CredentialTestNotSupportedCode()
        : base(20008, "CredentialTestNotSupported",
            ResultSeverities.ByName("Error"),
            "Testing unsaved connection '{name}' cannot use credentials; save it with its credentials, then test it",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
