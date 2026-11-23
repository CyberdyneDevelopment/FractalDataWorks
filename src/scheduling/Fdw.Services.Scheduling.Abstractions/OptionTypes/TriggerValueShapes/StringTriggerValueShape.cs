using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A string: read as a whole number, a date and time, or the words true and false, ignoring case.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "String")]
public sealed class StringTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="StringTriggerValueShape"/> class.</summary>
    public StringTriggerValueShape()
        : base("String")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is string;

    /// <inheritdoc />
    public override bool TryInt(object value, out int result) => int.TryParse((string)value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc />
    public override bool TryNarrowInt(object value, out int result) => TryInt(value, out result);

    /// <inheritdoc />
    public override bool TryDateTime(object value, out DateTime result) => DateTime.TryParse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    /// <inheritdoc />
    public override bool TryBool(object value, out bool result)
    {
        var text = (string)value;
        result = string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
        return result || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase);
    }
}
