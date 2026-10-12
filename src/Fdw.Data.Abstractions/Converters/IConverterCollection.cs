namespace Fdw.Data.Abstractions;

/// <summary>
/// Marker interface for domain-specific data type converter collections.
/// Collections implementing this interface will be aggregated by DataTypeConverters.
/// </summary>
/// <remarks>
/// <para>
/// This interface marks TypeCollection classes that contain domain-specific converters:
/// <list type="bullet">
/// <item>SqlConverters - SQL Server data type converters</item>
/// <item>JsonConverters - JSON data type converters</item>
/// <item>RestConverters - REST/HTTP data type converters</item>
/// </list>
/// </para>
/// <para>
/// The NestedTypeCollectionGenerator discovers all implementations and generates
/// nested access in the master DataTypeConverters class.
/// </para>
/// <para>
/// Note: Domain property should be implemented as a static property in the concrete class.
/// </para>
/// </remarks>
public interface IConverterCollection
{
    // Marker interface - no members
    // Domain property is implemented as static in concrete classes
}
