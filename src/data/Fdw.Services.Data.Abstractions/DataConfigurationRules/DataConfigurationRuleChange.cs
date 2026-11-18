using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>A change to a rule (FDW-6345): a field left out keeps the stored one, and <see cref="Clear"/> names the optional ones to empty. A concurrent change is last writer wins.</summary>
public sealed class DataConfigurationRuleChange
{
    /// <summary>Gets the rule.</summary>
    public required DataConfigurationRuleKey Key { get; init; }

    /// <summary>Gets the new name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the new abbreviation.</summary>
    public string? Abbreviation { get; init; }

    /// <summary>Gets the new description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the new column, within the same container.</summary>
    public string? Column { get; init; }

    /// <summary>Gets the new path's names.</summary>
    public IReadOnlyList<string>? Path { get; init; }

    /// <summary>Gets the new method.</summary>
    public string? Method { get; init; }

    /// <summary>Gets the new kind of owner.</summary>
    public string? OwnerKind { get; init; }

    /// <summary>Gets the new owner.</summary>
    public Guid? OwnerId { get; init; }

    /// <summary>Gets the new custodian.</summary>
    public Guid? CustodianUserId { get; init; }

    /// <summary>Gets the new required permission.</summary>
    public string? RequiredPermission { get; init; }

    /// <summary>Gets the optional fields to empty: description, requiredPermission.</summary>
    public IReadOnlyList<string>? Clear { get; init; }
}
