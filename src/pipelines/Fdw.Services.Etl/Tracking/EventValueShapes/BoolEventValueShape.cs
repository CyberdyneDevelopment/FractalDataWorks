using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A bool.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "Bool")]
public sealed class BoolEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="BoolEventValueShape"/> class.</summary>
    public BoolEventValueShape()
        : base("Bool")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is bool;

    /// <inheritdoc />
    public override bool? Flag(object value) => (bool)value;
}
