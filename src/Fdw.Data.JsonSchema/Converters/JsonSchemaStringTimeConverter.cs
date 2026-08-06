using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema string (time format) to CLR TimeOnly.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "StringTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaStringTimeConverter()
    : DataTypeConverterBase(
        id: 8,
        name: "StringTime",
        sourceType: "string+time",
        targetClrType: typeof(TimeOnly),
        dbType: DbType.Time)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is TimeOnly timeOnly)
        {
            return timeOnly;
        }

        if (dbValue is TimeSpan ts)
        {
            return TimeOnly.FromTimeSpan(ts);
        }

        return TimeOnly.Parse(dbValue.ToString()!, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
