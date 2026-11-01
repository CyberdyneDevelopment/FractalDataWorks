using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL nvarchar type: Unicode text, written UTF-16LE.
/// </summary>
/// <remarks>
/// Every text column this server writes is nvarchar, so no character value depends on a code page.
/// A value that is not a string is written as its invariant text when it has one.
/// </remarks>
[TypeOption(typeof(SqlDataTypes), "NVarChar", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlNVarCharType : SqlVariableLengthTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlNVarCharType"/> class.</summary>
    public SqlNVarCharType()
        : base(name: "NVarChar", sqlName: "nvarchar", systemTypeId: 231, typeId: TdsTypeIds.NVarChar, collated: true, bytesPerUnit: 2)
    {
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == typeof(string) || clrType == typeof(char);

    /// <inheritdoc/>
    protected override bool TryEncode(object value, out byte[] bytes)
    {
        var text = value switch
        {
            string s => s,
            char c => c.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => null,
        };

        bytes = text is null ? [] : Encoding.Unicode.GetBytes(text);
        return text is not null;
    }
}
