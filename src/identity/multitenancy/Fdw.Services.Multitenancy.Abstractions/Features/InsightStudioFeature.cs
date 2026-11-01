using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The Insight Studio feature (FDW-1580): explore data sets as charts, maps and flows in the studio.
/// </summary>
[TypeOption(typeof(Features), "InsightStudio")]
[ExcludeFromCodeCoverage]
public sealed class InsightStudioFeature : FeatureBase
{
    /// <summary>Initializes a new instance of the <see cref="InsightStudioFeature"/> class.</summary>
    public InsightStudioFeature()
        : base("InsightStudio", "Insight Studio", "Explore data sets as charts, maps and flows in the studio.")
    {
    }
}
