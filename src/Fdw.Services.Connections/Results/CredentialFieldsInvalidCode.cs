using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// The credentials a request sent do not match the ones the connection's authentication method uses (FDW-1086).
/// </summary>
/// <remarks>
/// Every credential field the method uses must be sent, and nothing else; a partial set would leave a connection that cannot open. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialFieldsInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialFieldsInvalidCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialFieldsInvalidCode"/> class.</summary>
    public CredentialFieldsInvalidCode()
        : base(20006, "CredentialFieldsInvalid",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' authenticates with '{method}', whose credentials are [{expected}]; the request sent [{supplied}]",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
