using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A DateTimeOffset, round-trip.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ParameterTextShapes), "Offset")]
public sealed class OffsetParameterTextShape : ParameterTextShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="OffsetParameterTextShape"/> class.</summary>
    public OffsetParameterTextShape()
        : base("Offset")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override string Write(object value) => ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture);
}
