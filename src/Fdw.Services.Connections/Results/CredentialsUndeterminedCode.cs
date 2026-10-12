using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// Which credentials a connection's authentication method uses could not be read from its configuration (FDW-1086).
/// </summary>
/// <remarks>
/// A configuration that does not say which authentication method it uses, or is of another kind, is refused rather than read as "no credentials". The reason says what is missing. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "CredentialsUndetermined", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CredentialsUndeterminedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CredentialsUndeterminedCode"/> class.</summary>
    public CredentialsUndeterminedCode()
        : base(20004, "CredentialsUndetermined",
            ResultSeverities.ByName("Error"),
            "Which stored credentials connection '{name}' uses cannot be read: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
