using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema number (double format) to CLR Double.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "NumberDouble", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaNumberDoubleConverter()
    : DataTypeConverterBase(
        id: 4,
        name: "NumberDouble",
        sourceType: "number+double",
        targetClrType: typeof(double),
        dbType: DbType.Double)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToDouble(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
