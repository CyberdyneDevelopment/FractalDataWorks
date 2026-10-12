namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// A spatial (geography/geometry) row value, carried above the connection layer as plain OGC WKT
/// text plus its SRID rather than a driver-specific UDT the caller cannot interpret.
/// </summary>
/// <param name="Wkt">OGC Well-Known Text for the value (e.g. <c>POINT (-95.37 29.76)</c>).</param>
/// <param name="Srid">The value's own spatial reference identifier — one column can mix SRIDs row to row.</param>
/// <param name="Kind">Either <see cref="Geography"/> or <see cref="Geometry"/>.</param>
public sealed record SpatialValue(string Wkt, int Srid, string Kind)
{
    /// <summary>The <see cref="Kind"/> value for a geography column.</summary>
    public const string Geography = "geography";

    /// <summary>The <see cref="Kind"/> value for a geometry column.</summary>
    public const string Geometry = "geometry";
}
