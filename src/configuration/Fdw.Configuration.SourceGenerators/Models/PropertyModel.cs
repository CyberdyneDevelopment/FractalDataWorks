using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration.SourceGenerators.Models;

/// <summary>
/// Analyzed model of a configuration property for code generation.
/// </summary>
/// <remarks>Excluded from coverage: pure data class with no logic.</remarks>
[ExcludeFromCodeCoverage]
public sealed class PropertyModel
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string PropertyName { get; set; } = "";

    /// <summary>
    /// Gets or sets the C# property type (e.g., "string", "int", "bool").
    /// </summary>
    public string PropertyType { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the property is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets whether the property is required (has [Required] attribute).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the max length for string properties.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the minimum value for numeric properties.
    /// </summary>
    public object? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for numeric properties.
    /// </summary>
    public object? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the precision for decimal properties.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale for decimal properties.
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether this property has a default value initializer.
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether this property is a collection.
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    /// Gets or sets the collection item type if IsCollection is true.
    /// </summary>
    public string? CollectionItemType { get; set; }

    /// <summary>
    /// Gets or sets whether this property is a complex type.
    /// </summary>
    public bool IsComplexType { get; set; }

    /// <summary>
    /// Gets or sets whether this property is an enum.
    /// </summary>
    public bool IsEnum { get; set; }

    /// <summary>
    /// Gets or sets the enum underlying type if IsEnum is true.
    /// </summary>
    public string? EnumUnderlyingType { get; set; }

    /// <summary>
    /// Gets or sets the column name override from [DbType].
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets whether this property should be excluded from DDL.
    /// </summary>
    public bool ExcludeFromDdl { get; set; }

    /// <summary>
    /// Gets or sets whether this column has a unique constraint.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets the index name if this column should be indexed.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Gets or sets the TypeCollection reference if this property references a TypeCollection.
    /// </summary>
    public TypeCollectionReference? TypeCollectionReference { get; set; }

    /// <summary>
    /// Gets or sets the ValuesFrom reference if this property has a [ValuesFrom] attribute.
    /// Indicates which TypeCollection provides the valid values for this property.
    /// </summary>
    public ValuesFromReference? ValuesFromReference { get; set; }

    /// <summary>
    /// Gets or sets the DbType override from [DbType] attribute.
    /// </summary>
    public DbTypeOverride? DbTypeOverride { get; set; }

    /// <summary>
    /// Gets or sets whether this property should be validated as an email address.
    /// </summary>
    public bool IsEmail { get; set; }

    /// <summary>
    /// Gets or sets whether this property should be validated as a URL.
    /// </summary>
    public bool IsUrl { get; set; }

    /// <summary>
    /// Gets or sets a regex pattern for validation.
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// Gets or sets whether this property is a navigation property — its type is another
    /// [ManagedConfiguration] class. Navigation properties represent parent-child
    /// relationships handled by separate child tables and must not become SQL columns.
    /// </summary>
    public bool IsNavigationProperty { get; set; }
}