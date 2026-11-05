using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Every kind of threshold (FDW-3948).</summary>
public sealed class ThresholdTypesResponse
{
    /// <summary>Gets or sets the kinds, by name.</summary>
    public IReadOnlyList<ThresholdTypeResponse> Types { get; set; } = [];
}
