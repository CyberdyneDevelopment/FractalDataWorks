using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A long.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "Long")]
public sealed class LongEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="LongEventValueShape"/> class.</summary>
    public LongEventValueShape()
        : base("Long")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is long;

    /// <inheritdoc />
    public override long? Number(object value) => (long)value;
}
