using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A connection was refused because it names a schema discovery option its connection type does not offer, or names none
/// (FDW-1051).
/// </summary>
/// <remarks>
/// Whoever saves a connection states its discovery option and nothing defaults it, so a request that omits it or names
/// the wrong one is refused. The reason names the option given, the connection type and the options that type offers, so
/// the caller can correct the request. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "SchemaDiscoveryNotOffered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SchemaDiscoveryNotOfferedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SchemaDiscoveryNotOfferedCode"/> class.</summary>
    public SchemaDiscoveryNotOfferedCode()
        : base(20001, "SchemaDiscoveryNotOffered",
            ResultSeverities.ByName("Error"),
            "Connection '{name}' cannot use schema discovery option '{option}': connection type '{connectionType}' offers {offered}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
