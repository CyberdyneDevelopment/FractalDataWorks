using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A map node was asked to explode into children the caller may see the node but not read: the fields of a data set need
/// the field-level read permission, which the map's own gate does not imply (FDW-1195).
/// </summary>
/// <remarks>
/// Refused, never answered with an empty set of children: a client must be able to say "you may not see this data
/// set's fields" and not "it has none". Category 10 (Forbidden), like <see cref="WorkspaceReadNotPermittedCode"/>. HTTP 403.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "MapExplodeNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MapExplodeNotPermittedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="MapExplodeNotPermittedCode"/> class.</summary>
    public MapExplodeNotPermittedCode()
        : base(100008, "MapExplodeNotPermitted",
            ResultSeverities.ByName("Error"),
            "Map node '{node}' cannot be exploded into {children} by this caller: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the explode was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
