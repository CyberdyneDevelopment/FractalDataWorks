using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>Text, parsed in the invariant culture.</summary>
[TypeOption(typeof(ReaderValueKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextReaderValueKind"/> class.</summary>
    public TextReaderValueKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string;

    /// <inheritdoc />
    public override Guid ToGuid(object value, string column) => Guid.Parse((string)value);

    /// <inheritdoc />
    public override string ToText(object value, string column) => (string)value;

    /// <inheritdoc />
    public override bool ToBoolean(object value)
    {
        var text = (string)value;
        if (bool.TryParse(text, out var parsedBool))
            return parsedBool;

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt)
            ? parsedInt != 0
            : base.ToBoolean(value);
    }

    /// <inheritdoc />
    public override DateTime ToDateTime(object value) => DateTime.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => decimal.Parse((string)value, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override DateTimeOffset ToDateTimeOffset(object value, string column) =>
        DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
