using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A map request was refused because what it names cannot be drawn: two scopes at once, a blank scope, or a node id
/// that is not one the map issues (FDW-1029).
/// </summary>
/// <remarks>
/// Refused rather than answered with the whole map. A request that names a scope and gets everything back has been
/// told something untrue about what it is looking at, and the whole map is the most expensive answer to give by
/// mistake. The reason names the part of the request at fault. HTTP 400.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "MapRequestInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MapRequestInvalidCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="MapRequestInvalidCode"/> class.</summary>
    public MapRequestInvalidCode()
        : base(20004, "MapRequestInvalid",
            ResultSeverities.ByName("Error"),
            "Map request rejected: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
