using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Lineage;

/// <summary>Base class for the directions a lineage walk can take.</summary>
public abstract class LineageDirectionBase : TypeOptionBase<LineageDirectionBase>, ILineageDirection
{
    /// <summary>Initializes a new instance of the <see cref="LineageDirectionBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="wire">The word a request names the direction by.</param>
    /// <param name="includesUpstream">Whether the walk goes toward what feeds the focus.</param>
    /// <param name="includesDownstream">Whether the walk goes toward what the focus feeds.</param>
    protected LineageDirectionBase(string name, string wire, bool includesUpstream, bool includesDownstream)
        : base(name)
    {
        Wire = wire;
        IncludesUpstream = includesUpstream;
        IncludesDownstream = includesDownstream;
    }

    /// <inheritdoc />
    [TypeLookup("ByWire")]
    public string Wire { get; }

    /// <inheritdoc />
    public bool IncludesUpstream { get; }

    /// <inheritdoc />
    public bool IncludesDownstream { get; }
}
