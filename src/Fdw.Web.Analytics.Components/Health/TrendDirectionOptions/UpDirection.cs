using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Analytics.Components.Health.TrendDirectionOptions;

/// <summary>
/// Up trend direction - values are increasing.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TrendDirections), "Up", RestrictToCurrentCompilation = true)]
public sealed class UpDirection : TrendDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpDirection"/> class.
    /// </summary>
    public UpDirection() : base(1, "Up")
    {
    }
}
