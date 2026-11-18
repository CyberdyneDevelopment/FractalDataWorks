using System;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>What <see cref="IDataConfigurationRules.List"/> is asked (FDW-6345).</summary>
public sealed class DataConfigurationRulesQuery
{
    /// <summary>Gets the container whose rules are listed.</summary>
    public required DataConfigurationRuleContainer Target { get; init; }

    /// <summary>Gets the column whose rules are listed; null lists every column's.</summary>
    public string? Column { get; init; }

    /// <summary>Gets whether to list removed rules or current ones; it has no default, so a query that does not say is refused.</summary>
    public bool? Removed { get; init; }

    /// <summary>Gets the kind of owner to list; null lists every owner's.</summary>
    public string? OwnerKind { get; init; }

    /// <summary>Gets the owner to list; null lists every owner's.</summary>
    public Guid? OwnerId { get; init; }

    /// <summary>Gets how many rules to pass over, as the platform's page settings sized it.</summary>
    public required int Skip { get; init; }

    /// <summary>Gets how many rules the page holds, as the platform's page settings sized it.</summary>
    public required int Take { get; init; }
}
