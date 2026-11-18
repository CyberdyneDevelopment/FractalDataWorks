using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>A data configuration rule (FDW-6345): where its values come from (one container and column), which values were picked, and the path that says what a consumer does with them.</summary>
public sealed class DataConfigurationRule
{
    /// <summary>Gets the rule's durable id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the rule's name, unique among the current rules of its column.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the rule's abbreviation, unique among the current rules of its column.</summary>
    public required string Abbreviation { get; init; }

    /// <summary>Gets what the rule is for.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the container and its data store and path.</summary>
    public required DataConfigurationRuleContainer Target { get; init; }

    /// <summary>Gets the column the rule is about.</summary>
    public required string Column { get; init; }

    /// <summary>Gets the path's names, from the DataConfigurationRules root to the kind.</summary>
    public required IReadOnlyList<string> Path { get; init; }

    /// <summary>Gets the method the path ends in, after the period.</summary>
    public required string Method { get; init; }

    /// <summary>Gets the rule's values, in ordinal order.</summary>
    public required IReadOnlyList<DataConfigurationRuleValue> Values { get; init; }

    /// <summary>Gets the kind of owner, a name in the DataOptionOwnerKinds collection.</summary>
    public required string OwnerKind { get; init; }

    /// <summary>Gets the owning organization or workspace.</summary>
    public required Guid OwnerId { get; init; }

    /// <summary>Gets the custodian: the person who controls the rule.</summary>
    public required Guid CustodianUserId { get; init; }

    /// <summary>Gets whether the custodian still holds datastores:write, a name in the DataOptionCustodianStates collection (FDW-6346).</summary>
    public required string CustodianState { get; init; }

    /// <summary>Gets the permission a person other than the custodian must hold to change the rule.</summary>
    public string? RequiredPermission { get; init; }

    /// <summary>Gets whether the rule is switched on.</summary>
    public required bool Active { get; init; }

    /// <summary>Gets whether the rule is removed; a removed rule is kept and can be restored.</summary>
    public required bool Removed { get; init; }

    /// <summary>Gets whether the caller may change the rule: the custodian, or a holder of the rule's required permission (FDW-6346).</summary>
    public required bool CanChange { get; init; }

    /// <summary>
    /// Gets the person who last changed the rule, from the administrative audit (FDW-6346): the actor of the rule's newest recorded change. It is null for a
    /// rule with no recorded change (one that was written by a migration or by hand), and is never stood in for by the database login that wrote the row.
    /// </summary>
    public Guid? ChangedByUserId { get; init; }

    /// <summary>Gets when the rule was last changed.</summary>
    public required DateTimeOffset ChangedAt { get; init; }
}
