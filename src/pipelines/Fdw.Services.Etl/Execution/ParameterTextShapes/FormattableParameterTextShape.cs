using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>Any other value that formats itself, such as a number.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ParameterTextShapes), "Formattable")]
public sealed class FormattableParameterTextShape : ParameterTextShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="FormattableParameterTextShape"/> class.</summary>
    public FormattableParameterTextShape()
        : base("Formattable")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is IFormattable and not DateTimeOffset and not DateTime;

    /// <inheritdoc />
    public override string Write(object value) => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
}
