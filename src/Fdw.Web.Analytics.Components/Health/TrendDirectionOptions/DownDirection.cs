using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Analytics.Components.Health.TrendDirectionOptions;

/// <summary>
/// Down trend direction - values are decreasing.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TrendDirections), "Down", RestrictToCurrentCompilation = true)]
public sealed class DownDirection : TrendDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DownDirection"/> class.
    /// </summary>
    public DownDirection() : base(2, "Down")
    {
    }
}
