using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Blue (info/running).</summary>
[TypeOption(typeof(StatusColors), "Blue")]
[ExcludeFromCodeCoverage]
public sealed class BlueStatusColor : StatusColorBase
{
    /// <summary>Initializes a new instance of <see cref="BlueStatusColor"/>.</summary>
    public BlueStatusColor() : base(3, "Blue", "dot-glacier", "var(--glacier)", "var(--glacier)") { }
}
