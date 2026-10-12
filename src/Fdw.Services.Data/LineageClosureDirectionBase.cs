using Fdw.Collections;

namespace Fdw.Services.Data;

/// <summary>
/// Base class for lineage closure read directions. Replaces the plain
/// <c>LineageClosureDirection</c> enum so each direction carries its own filter property name instead
/// of a switch/ternary at the read site.
/// </summary>
public abstract class LineageClosureDirectionBase : TypeOptionBase<LineageClosureDirectionBase>, ILineageClosureDirection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineageClosureDirectionBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="closureIdPropertyName">
    /// The <see cref="DataSetLineageClosureRow"/> property this direction filters on.
    /// </param>
    protected LineageClosureDirectionBase(string name, string closureIdPropertyName)
        : base(name)
    {
        ClosureIdPropertyName = closureIdPropertyName;
    }

    /// <inheritdoc />
    public string ClosureIdPropertyName { get; }
}
