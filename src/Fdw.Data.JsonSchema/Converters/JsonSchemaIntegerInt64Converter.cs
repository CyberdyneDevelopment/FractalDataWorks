using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema integer (int64 format) to CLR Int64.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "IntegerInt64", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaIntegerInt64Converter()
    : DataTypeConverterBase(
        id: 2,
        name: "IntegerInt64",
        sourceType: "integer+int64",
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
