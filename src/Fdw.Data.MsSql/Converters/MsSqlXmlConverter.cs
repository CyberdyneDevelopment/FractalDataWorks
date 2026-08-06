using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL xml to CLR String.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Xml", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlXmlConverter()
    : DataTypeConverterBase(
        id: 23,
        name: "Xml",
        sourceType: "xml",
        targetClrType: typeof(string),
        dbType: DbType.Xml)
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
