using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// An update stated a member that the connection does not store, so there is nothing for it to change (FDW-1177).
/// </summary>
/// <remarks>
/// The update request carries <c>IsActive</c>, but a connection has no such setting: the detail response reports it as a constant. An update that
/// accepted the member would answer success and change nothing, so it is refused and names the member. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "ConnectionMemberNotSettable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionMemberNotSettableCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionMemberNotSettableCode"/> class.</summary>
    public ConnectionMemberNotSettableCode()
        : base(20009, "ConnectionMemberNotSettable",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' update states '{property}', which a connection does not store, so it cannot be changed; leave it out of the request",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller which member was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
