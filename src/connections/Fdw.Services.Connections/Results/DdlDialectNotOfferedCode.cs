using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A request was refused because the connection type it names offers no DDL dialect: it has no generator for its engine's
/// DDL text (FDW-5959).
/// </summary>
/// <remarks>
/// A connection type states its dialect through <c>IConnectionType.DdlGenerator</c> and nothing defaults it, so an engine that
/// cannot say its dialect is refused, never given another engine's text. The reason names the connection type so the caller
/// can see which engine has none. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "DdlDialectNotOffered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DdlDialectNotOfferedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DdlDialectNotOfferedCode"/> class.</summary>
    public DdlDialectNotOfferedCode()
        : base(20015, "DdlDialectNotOffered",
            ResultSeverities.ByName("Error"),
            "Connection type '{connectionType}' offers no DDL dialect: it has no generator for its engine's DDL text",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
