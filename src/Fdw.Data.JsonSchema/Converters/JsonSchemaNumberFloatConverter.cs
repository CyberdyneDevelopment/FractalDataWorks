using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.JsonSchema;

/// <summary>
/// Converts JSON Schema number (float format) to CLR Single.
/// </summary>
[TypeOption(typeof(JsonSchemaConverters), "NumberFloat", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JsonSchemaNumberFloatConverter()
    : DataTypeConverterBase(
        id: 3,
        name: "NumberFloat",
        sourceType: "number+float",
        targetClrType: typeof(float),
        dbType: DbType.Single)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToSingle(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
