using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>A change to one value of a rule (FDW-6345): a field left out keeps the stored one, and <see cref="Clear"/> names the optional ones to empty.</summary>
public sealed class DataConfigurationRuleValueChange
{
    /// <summary>Gets the rule the value belongs to.</summary>
    public required DataConfigurationRuleKey Key { get; init; }

    /// <summary>Gets the value's durable id.</summary>
    public required Guid ValueId { get; init; }

    /// <summary>Gets the new kind.</summary>
    public string? Kind { get; init; }

    /// <summary>Gets the new literal.</summary>
    public string? Value { get; init; }

    /// <summary>Gets the new parameter name.</summary>
    public string? Parameter { get; init; }

    /// <summary>Gets the new name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the new abbreviation.</summary>
    public string? Abbreviation { get; init; }

    /// <summary>Gets the new description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the new ordinal.</summary>
    public int? Ordinal { get; init; }

    /// <summary>Gets the optional members to empty: name, abbreviation, description.</summary>
    public IReadOnlyList<string>? Clear { get; init; }
}
