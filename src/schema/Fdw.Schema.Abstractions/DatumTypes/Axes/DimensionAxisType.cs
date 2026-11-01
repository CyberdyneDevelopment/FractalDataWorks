using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A field a chart splits by: a category, a name, a group.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AxisTypes), "Dimension")]
public sealed class DimensionAxisType : AxisTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DimensionAxisType"/> class.</summary>
    public DimensionAxisType() : base("Dimension", "What a chart splits its marks by.")
    {
    }
}
