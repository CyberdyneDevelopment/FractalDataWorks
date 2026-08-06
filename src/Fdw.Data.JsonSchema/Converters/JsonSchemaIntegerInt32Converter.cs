using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema integer (int32 format) to CLR Int32.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "IntegerInt32", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaIntegerInt32Converter()
    : DataTypeConverterBase(
        id: 1,
        name: "IntegerInt32",
        sourceType: "integer+int32",
        targetClrType: typeof(int),
        dbType: DbType.Int32)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToInt32(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
