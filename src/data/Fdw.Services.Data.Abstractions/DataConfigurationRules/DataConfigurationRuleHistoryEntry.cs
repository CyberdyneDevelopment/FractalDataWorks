using System;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>One recorded change of a data configuration rule (FDW-6571): who made it, when, to which rule, what kind of change it was and, for a change of one member, the old and new text.</summary>
public sealed class DataConfigurationRuleHistoryEntry
{
    /// <summary>Gets when the change was made.</summary>
    public required DateTimeOffset At { get; init; }

    /// <summary>Gets who made the change.</summary>
    public required Guid ByUserId { get; init; }

    /// <summary>Gets the name of who made the change, as the audit recorded it.</summary>
    public required string ByName { get; init; }

    /// <summary>Gets the rule the change was made to.</summary>
    public required Guid RuleId { get; init; }

    /// <summary>Gets the rule's name now.</summary>
    public required string RuleName { get; init; }

    /// <summary>Gets the kind of change, from <see cref="DataConfigurationRuleCatalog"/>.</summary>
    public required DataConfigurationRuleChangeKind Kind { get; init; }

    /// <summary>Gets the text before the change; absent when the change added something.</summary>
    public string? OldValue { get; init; }

    /// <summary>Gets the text after the change; absent when the change removed something.</summary>
    public string? NewValue { get; init; }
}
