using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>No trend data available.</summary>
[TypeOption(typeof(TrendDirections), "None")]
[ExcludeFromCodeCoverage]
public sealed class NoneTrendDirection : TrendDirectionBase
{
    /// <summary>Initializes a new instance of <see cref="NoneTrendDirection"/>.</summary>
    public NoneTrendDirection() : base(1, "None") { }
}
