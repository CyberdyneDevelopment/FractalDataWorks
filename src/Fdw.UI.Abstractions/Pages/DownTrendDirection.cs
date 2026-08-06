using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Value is decreasing.</summary>
[TypeOption(typeof(TrendDirections), "Down")]
[ExcludeFromCodeCoverage]
public sealed class DownTrendDirection : TrendDirectionBase
{
    /// <summary>Initializes a new instance of <see cref="DownTrendDirection"/>.</summary>
    public DownTrendDirection() : base(3, "Down") { }
}
