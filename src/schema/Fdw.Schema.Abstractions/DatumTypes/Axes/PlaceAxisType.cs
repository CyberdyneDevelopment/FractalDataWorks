using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A field that says where: a district, a zip code, a state, a longitude or a latitude.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AxisTypes), "Place")]
public sealed class PlaceAxisType : AxisTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PlaceAxisType"/> class.</summary>
    public PlaceAxisType() : base("Place", "Where something is.")
    {
    }
}
