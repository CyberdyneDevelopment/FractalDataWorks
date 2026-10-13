using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A connection names an external secret manager or secret that the platform cannot read (FDW-1327).
/// </summary>
/// <remarks>
/// Naming an external credential checks that the secret manager is registered and that each named secret can be read; the value is not kept or logged. The reason is the store's own result code, or that the secret manager is not registered. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "ExternalCredentialUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExternalCredentialUnavailableCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ExternalCredentialUnavailableCode"/> class.</summary>
    public ExternalCredentialUnavailableCode()
        : base(20012, "ExternalCredentialUnavailable",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' names secret manager '{secretManager}' for its '{field}' credential, but it cannot be used: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
