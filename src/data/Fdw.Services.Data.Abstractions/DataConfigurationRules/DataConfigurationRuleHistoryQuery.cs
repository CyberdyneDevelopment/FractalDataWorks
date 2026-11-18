using System;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>What to list of the recorded changes of a container's rules (FDW-6571).</summary>
public sealed class DataConfigurationRuleHistoryQuery
{
    /// <summary>Gets the container whose rules' changes are listed.</summary>
    public required DataConfigurationRuleContainer Target { get; init; }

    /// <summary>Gets the page, counted from one.</summary>
    public required int Page { get; init; }

    /// <summary>Gets how many changes a page holds at most.</summary>
    public required int PageSize { get; init; }

    /// <summary>Gets the column whose rules' changes are listed; null lists every column's.</summary>
    public string? Column { get; init; }

    /// <summary>Gets the one rule whose changes are listed; null lists every rule's.</summary>
    public Guid? RuleId { get; init; }
}
