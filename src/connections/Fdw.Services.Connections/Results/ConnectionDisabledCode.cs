using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A connection was asked for while it is disabled (FDW-3951).
/// </summary>
/// <remarks>
/// A connection is disabled by a threshold that fired or by a person, and stays so until a person enables it again.
/// Everything that resolves a connection (data access, pipelines, schema discovery, the health monitor) is refused with this
/// code. The details name the connection, when it was disabled and why, so the caller can tell the operator what to look at.
/// The number puts the code in the Refused category (12, FDW-4005): a run refused because its connection is disabled is not the fault of the pipeline that ran, so the failure
/// counter of FDW-3864 does not count it and no pipeline is disabled for it.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "ConnectionDisabled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionDisabledCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionDisabledCode"/> class.</summary>
    public ConnectionDisabledCode()
        : base(120000, "ConnectionDisabled",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' has been disabled since {disabledAt}: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
