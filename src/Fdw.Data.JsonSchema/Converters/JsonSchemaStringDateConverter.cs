using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema string (date format) to CLR DateOnly.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "StringDate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaStringDateConverter()
    : DataTypeConverterBase(
        id: 7,
        name: "StringDate",
        sourceType: "string+date",
        targetClrType: typeof(DateOnly),
        dbType: DbType.Date)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is DateOnly dateOnly)
        {
            return dateOnly;
        }

        if (dbValue is DateTime dt)
        {
            return DateOnly.FromDateTime(dt);
        }

        return DateOnly.Parse(dbValue.ToString()!, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
