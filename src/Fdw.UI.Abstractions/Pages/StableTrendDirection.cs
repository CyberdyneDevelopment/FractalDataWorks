using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Value is stable.</summary>
[TypeOption(typeof(TrendDirections), "Stable")]
[ExcludeFromCodeCoverage]
public sealed class StableTrendDirection : TrendDirectionBase
{
    /// <summary>Initializes a new instance of <see cref="StableTrendDirection"/>.</summary>
    public StableTrendDirection() : base(4, "Stable") { }
}
