using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A connection whose authentication method uses stored credentials has none for some of them (FDW-1086).
/// </summary>
/// <remarks>
/// Saving it would leave a connection that cannot open, so the save is refused. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialRequiredCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialRequiredCode"/> class.</summary>
    public CredentialRequiredCode()
        : base(20007, "CredentialRequired",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' authenticates with '{method}', which needs credentials [{missing}]; send them in 'credentials'",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
