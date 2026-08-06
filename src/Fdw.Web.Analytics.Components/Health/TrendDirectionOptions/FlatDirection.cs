using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Analytics.Components.Health.TrendDirectionOptions;

/// <summary>
/// Flat trend direction - no significant change.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TrendDirections), "Flat", RestrictToCurrentCompilation = true)]
public sealed class FlatDirection : TrendDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlatDirection"/> class.
    /// </summary>
    public FlatDirection() : base(0, "Flat")
    {
    }
}
