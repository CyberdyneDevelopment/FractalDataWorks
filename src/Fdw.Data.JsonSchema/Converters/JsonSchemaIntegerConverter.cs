using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema integer with no format to CLR Int64. JSON Schema integers carry no bound, so the widest integer type holds them.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "Integer", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaIntegerConverter()
    : DataTypeConverterBase(
        name: "Integer",
        sourceType: "integer",
        targetClrType: typeof(long),
        dbType: DbType.Int64)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToInt64(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
