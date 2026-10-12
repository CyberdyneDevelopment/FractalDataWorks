using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// Whether a connection presents the host's own identity could not be decided from its configuration (FDW-1068).
/// </summary>
/// <remarks>
/// The answer gates who may save the connection, so a configuration that does not say enough -- an authentication
/// method its kind does not know, or none -- is refused rather than read as "no". The reason says what is missing.
/// HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "HostIdentityUndetermined", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HostIdentityUndeterminedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="HostIdentityUndeterminedCode"/> class.</summary>
    public HostIdentityUndeterminedCode()
        : base(20003, "HostIdentityUndetermined",
            ResultSeverities.ByName("Error"),
            "Whether connection '{name}' connects as the host's own identity cannot be decided: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
