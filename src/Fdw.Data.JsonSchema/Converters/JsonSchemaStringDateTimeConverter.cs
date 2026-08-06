using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema string (date-time format) to CLR DateTime.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "StringDateTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaStringDateTimeConverter()
    : DataTypeConverterBase(
        id: 6,
        name: "StringDateTime",
        sourceType: "string+date-time",
        targetClrType: typeof(DateTime),
        dbType: DbType.DateTime)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is DateTime dt)
        {
            return dt;
        }

        return DateTime.Parse(dbValue.ToString()!, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
