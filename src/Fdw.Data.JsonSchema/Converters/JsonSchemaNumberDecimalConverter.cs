using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema number (default/no format) to CLR Decimal.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "NumberDecimal", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaNumberDecimalConverter()
    : DataTypeConverterBase(
        id: 5,
        name: "NumberDecimal",
        sourceType: "number",
        targetClrType: typeof(decimal),
        dbType: DbType.Decimal)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToDecimal(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
