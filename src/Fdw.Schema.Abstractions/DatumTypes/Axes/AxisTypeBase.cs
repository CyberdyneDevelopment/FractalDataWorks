using Fdw.Collections;

namespace Fdw.Schema;

/// <summary>Base for the axes a field's data can sit on in a chart.</summary>
/// <remarks>
/// No id is passed: <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully qualified type name.
/// A chart slot and a datum type cite an axis by option name; the names are not persisted by themselves.
/// </remarks>
public abstract class AxisTypeBase : TypeOptionBase<AxisTypeBase>, IAxisType
{
    /// <summary>Initializes a new instance of the <see cref="AxisTypeBase"/> class.</summary>
    /// <param name="name">The axis name, as a chart slot and a datum type cite it.</param>
    /// <param name="description">What a field on this axis is for.</param>
    protected AxisTypeBase(string name, string description) : base(name, name, description, null)
    {
    }
}
