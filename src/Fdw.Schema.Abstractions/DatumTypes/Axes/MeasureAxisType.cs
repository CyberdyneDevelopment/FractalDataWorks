using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A number a chart plots: summed, averaged, counted or taken as it stands.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AxisTypes), "Measure")]
public sealed class MeasureAxisType : AxisTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MeasureAxisType"/> class.</summary>
    public MeasureAxisType() : base("Measure", "A number a chart plots.")
    {
    }
}
