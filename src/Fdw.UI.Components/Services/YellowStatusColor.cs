using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Yellow (warning/degraded).</summary>
[TypeOption(typeof(StatusColors), "Yellow")]
[ExcludeFromCodeCoverage]
public sealed class YellowStatusColor : StatusColorBase
{
    /// <summary>Initializes a new instance of <see cref="YellowStatusColor"/>.</summary>
    public YellowStatusColor() : base(4, "Yellow", "dot-amber", "var(--warn)", "var(--warn)") { }
}
