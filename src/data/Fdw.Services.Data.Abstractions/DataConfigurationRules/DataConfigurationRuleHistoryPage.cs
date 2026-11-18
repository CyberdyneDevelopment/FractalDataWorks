using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>One page of the recorded changes of a container's rules, newest first (FDW-6571).</summary>
public sealed class DataConfigurationRuleHistoryPage
{
    /// <summary>Gets the changes of the page.</summary>
    public required IReadOnlyList<DataConfigurationRuleHistoryEntry> Items { get; init; }

    /// <summary>Gets how many changes the audit holds for the rules in all.</summary>
    public required int TotalCount { get; init; }
}
