using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>A connection kind was asked for its implementation provider but is the empty kind the collection returns for a name nobody registered (FDW-6360).</summary>
[TypeOption(typeof(ConnectionsResultCodes), "ConnectionKindNotRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionKindNotRegisteredCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionKindNotRegisteredCode"/> class.</summary>
    public ConnectionKindNotRegisteredCode()
        : base(20016, "ConnectionKindNotRegistered",
            ResultSeverities.ByName("Error"),
            "No connection kind is registered under the name asked for, so it has no implementation provider to build",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
