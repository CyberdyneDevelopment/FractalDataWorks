using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.TestDouble;

/// <summary>A third party's own service for its component (FDW-6240).</summary>
[ExcludeFromCodeCoverage]
public sealed class LookupContribution : IFlowBuildContribution
{
    /// <summary>Gets or sets a value indicating whether a build read this contribution.</summary>
    public bool Used { get; set; }
}
