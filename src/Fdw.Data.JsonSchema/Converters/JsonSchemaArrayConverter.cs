using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema array to CLR String (serialized JSON).
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "Array", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaArrayConverter()
    : DataTypeConverterBase(
        id: 12,
        name: "Array",
        sourceType: "array",
        targetClrType: typeof(string),
        dbType: DbType.String)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return dbValue.ToString();
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
