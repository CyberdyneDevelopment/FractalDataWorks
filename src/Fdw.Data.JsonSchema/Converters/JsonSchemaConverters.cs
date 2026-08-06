using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// TypeCollection for JSON Schema data type converters.
/// Child collection of DataTypeConverters.
/// Provides BySourceType() lookup for composite keys (type+format).
/// </summary>
[TypeOption(typeof(DataTypeConverters), "JsonSchema", RestrictToCurrentCompilation = true)]
[TypeCollection(typeof(DataTypeConverterBase),
                typeof(IDataTypeConverter),
                typeof(JsonSchemaConverters))]
public abstract partial class JsonSchemaConverters : DataTypeConverterCollectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaConverters"/> class.
    /// </summary>
    protected JsonSchemaConverters() : base("JsonSchema", "JSON Schema")
    {
    }
    // Source generator creates:
    // - public static IDataTypeConverter IntegerInt32 { get; }
    // - public static IDataTypeConverter IntegerInt64 { get; }
    // - public static IDataTypeConverter String { get; }
    // - ... (properties for all 13 converters)
    // - public static IDataTypeConverter ById(int id) { ... }
    // - public static IDataTypeConverter ByName(string name) { ... }
    // - public static IDataTypeConverter NotFound { get; }
    // - public static IReadOnlyList<IDataTypeConverter> All() { ... }

    /// <summary>
    /// Gets a converter by JSON Schema source type (e.g., "integer+int64", "string+date-time", "boolean").
    /// Returns NotFound if not found.
    /// </summary>
    public static IDataTypeConverter BySourceType(string sourceType)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
        {
            return NotFound;
        }

        var converter = All().FirstOrDefault(c =>
            c.SourceType.Equals(sourceType, System.StringComparison.Ordinal));

        return converter ?? NotFound;
    }
}
