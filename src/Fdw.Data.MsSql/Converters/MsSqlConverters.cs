using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// TypeCollection for Microsoft SQL Server data type converters.
/// Child collection of DataTypeConverters.
/// Provides BySourceType() lookup via TypeLookup attribute on DataTypeConverterBase.SourceType.
/// </summary>
[TypeOption(typeof(DataTypeConverters), "MsSql", RestrictToCurrentCompilation = true)]
[TypeCollection(typeof(DataTypeConverterBase),
                typeof(IDataTypeConverter),
                typeof(MsSqlConverters))]
public abstract partial class MsSqlConverters : DataTypeConverterCollectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConverters"/> class.
    /// </summary>
    protected MsSqlConverters() : base("MsSql", "Microsoft SQL Server")
    {
    }
    // Source generator creates:
    // - public static IDataTypeConverter Int32 { get; }
    // - public static IDataTypeConverter Int64 { get; }
    // - public static IDataTypeConverter String { get; }
    // - ... (properties for all 10 converters)
    // - public static IDataTypeConverter ById(int id) { ... }
    // - public static IDataTypeConverter ByName(string name) { ... }
    // - public static IDataTypeConverter NotFound { ... }
    // - public static IReadOnlyList<IDataTypeConverter> All() { ... }

    /// <summary>
    /// Gets a converter by SQL source type name (e.g., "int", "nvarchar", "bigint").
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
