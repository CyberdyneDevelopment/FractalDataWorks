using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>
/// A request was refused because the connection type it names offers no DDL execution: DDL cannot be run on a connection of this kind
/// (FDW-3617).
/// </summary>
/// <remarks>
/// A connection type states that it runs DDL by implementing <c>IDdlExecution</c> and nothing defaults it, so a kind that does not is
/// refused, never run through another kind's way. The reason names the connection type so the caller can see which engine has none. HTTP 400.
/// </remarks>
[TypeOption(typeof(ConnectionsResultCodes), "DdlExecutionNotOffered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DdlExecutionNotOfferedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DdlExecutionNotOfferedCode"/> class.</summary>
    public DdlExecutionNotOfferedCode()
        : base(20019, "DdlExecutionNotOffered",
            ResultSeverities.ByName("Error"),
            "Connection type '{connectionType}' offers no DDL execution: DDL cannot be run on a connection of this kind",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details the message is filled from.</param>
    /// <returns>The message for the caller.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
