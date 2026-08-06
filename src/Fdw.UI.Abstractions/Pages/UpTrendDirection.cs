using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Value is increasing.</summary>
[TypeOption(typeof(TrendDirections), "Up")]
[ExcludeFromCodeCoverage]
public sealed class UpTrendDirection : TrendDirectionBase
{
    /// <summary>Initializes a new instance of <see cref="UpTrendDirection"/>.</summary>
    public UpTrendDirection() : base(2, "Up") { }
}
