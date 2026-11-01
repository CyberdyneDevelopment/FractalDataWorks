using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Maps feature states to their responses (FDW-1580).
/// </summary>
internal static class FeatureMap
{
    internal static FeatureResponse Of(FeatureState state)
        => new()
        {
            Name = state.Name, DisplayName = state.DisplayName, Description = state.Description, Enabled = state.Enabled, OffAt = state.OffAt,
        };

    internal static FeaturesResponse Of(IReadOnlyList<FeatureState> states)
        => new() { Features = [.. states.Select(state => Of(state))] };
}
