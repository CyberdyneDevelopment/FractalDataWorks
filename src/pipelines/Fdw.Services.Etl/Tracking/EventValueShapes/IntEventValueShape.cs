using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>An int.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "Int")]
public sealed class IntEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="IntEventValueShape"/> class.</summary>
    public IntEventValueShape()
        : base("Int")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is int;

    /// <inheritdoc />
    public override long? Number(object value) => (int)value;
}
