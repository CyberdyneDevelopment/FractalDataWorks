using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema boolean to CLR Boolean.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "Boolean", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaBooleanConverter()
    : DataTypeConverterBase(
        id: 11,
        name: "Boolean",
        sourceType: "boolean",
        targetClrType: typeof(bool),
        dbType: DbType.Boolean)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToBoolean(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
