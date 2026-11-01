using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A number, read as a whole number or a decimal.</summary>
[TypeOption(typeof(XlsxValueKinds), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberXlsxValueKind : XlsxValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberXlsxValueKind"/> class.</summary>
    public NumberXlsxValueKind() : base("Number")
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// A whole number stays whole, so an id of 12 is a long and not 12.0; anything with a fraction or an exponent is a
    /// decimal, and only a value beyond decimal's range falls to a double.
    /// </remarks>
    public override object Read(string text, bool date1904)
    {
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole))
        {
            return whole;
        }

        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var big)
            ? big
            : throw new InvalidDataException($"'{text}' is not a number.");
    }
}
