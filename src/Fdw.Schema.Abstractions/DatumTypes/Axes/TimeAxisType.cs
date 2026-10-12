using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A field that says when: a point in time, or either end of a validity period.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AxisTypes), "Time")]
public sealed class TimeAxisType : AxisTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeAxisType"/> class.</summary>
    public TimeAxisType() : base("Time", "When something happened.")
    {
    }
}
