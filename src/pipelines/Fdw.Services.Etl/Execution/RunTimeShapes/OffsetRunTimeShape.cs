using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A DateTimeOffset, as it is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RunTimeShapes), "Offset")]
public sealed class OffsetRunTimeShape : RunTimeShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="OffsetRunTimeShape"/> class.</summary>
    public OffsetRunTimeShape()
        : base("Offset")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object? value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override DateTimeOffset ToOffset(object value) => (DateTimeOffset)value;
}
