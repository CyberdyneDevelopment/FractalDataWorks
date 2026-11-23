using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A moment.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "Moment")]
public sealed class MomentEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="MomentEventValueShape"/> class.</summary>
    public MomentEventValueShape()
        : base("Moment")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override DateTimeOffset? Time(object value) => (DateTimeOffset)value;
}
