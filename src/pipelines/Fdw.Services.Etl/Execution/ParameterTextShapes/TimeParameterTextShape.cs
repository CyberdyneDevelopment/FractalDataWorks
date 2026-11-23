using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A DateTime, round-trip.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ParameterTextShapes), "Time")]
public sealed class TimeParameterTextShape : ParameterTextShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeParameterTextShape"/> class.</summary>
    public TimeParameterTextShape()
        : base("Time")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is DateTime;

    /// <inheritdoc />
    public override string Write(object value) => ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);
}
