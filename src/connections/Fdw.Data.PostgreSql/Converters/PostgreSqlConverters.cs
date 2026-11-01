using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// TypeCollection for PostgreSQL data type converters.
/// Child collection of DataTypeConverters.
/// Provides BySourceType() lookup via TypeLookup attribute on DataTypeConverterBase.SourceType.
/// </summary>
[TypeOption(typeof(DataTypeConverters), "PostgreSql", RestrictToCurrentCompilation = true)]
[TypeCollection(typeof(DataTypeConverterBase),
                typeof(IDataTypeConverter),
                typeof(PostgreSqlConverters))]
public abstract partial class PostgreSqlConverters : DataTypeConverterCollectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConverters"/> class.
    /// </summary>
    protected PostgreSqlConverters() : base("PostgreSql", "PostgreSQL")
    {
    }

    /// <summary>
    /// Gets a converter by SQL source type name (e.g., "integer", "text", "boolean").
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
