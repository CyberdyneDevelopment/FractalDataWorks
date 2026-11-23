using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A geometry or geography value, written as EWKT: the SRID is part of what the value means, and a coordinate pair without the reference system it was measured in is not the same value.</summary>
[TypeOption(typeof(RecordValueKinds), "Spatial")]
[ExcludeFromCodeCoverage]
public sealed class SpatialRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="SpatialRecordValueKind"/> class.</summary>
    public SpatialRecordValueKind()
        : base("Spatial")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is SpatialValue;

    /// <inheritdoc />
    public override string Text(object value) => string.Concat("SRID=", ((SpatialValue)value).Srid.ToString(CultureInfo.InvariantCulture), ";", ((SpatialValue)value).Wkt);
}
