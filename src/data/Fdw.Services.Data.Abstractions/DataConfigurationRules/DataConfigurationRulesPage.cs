using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>One page of a container's rules (FDW-6345).</summary>
public sealed class DataConfigurationRulesPage
{
    /// <summary>Gets the rules, in column then name order.</summary>
    public required IReadOnlyList<DataConfigurationRule> Items { get; init; }

    /// <summary>Gets how many rules the query matches in all, counted, not worked out from whether a next page exists.</summary>
    public required int TotalCount { get; init; }
}
