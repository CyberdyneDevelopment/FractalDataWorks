using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>A rule to add (FDW-6345). Nothing has a default: a field a rule needs and this leaves out is refused with its own code.</summary>
public sealed class NewDataConfigurationRule
{
    /// <summary>Gets the container the rule is added under.</summary>
    public required DataConfigurationRuleContainer Target { get; init; }

    /// <summary>Gets the rule's name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the rule's abbreviation.</summary>
    public string? Abbreviation { get; init; }

    /// <summary>Gets what the rule is for.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the column the rule is about.</summary>
    public string? Column { get; init; }

    /// <summary>Gets the path's names, from the DataConfigurationRules root to the kind.</summary>
    public IReadOnlyList<string>? Path { get; init; }

    /// <summary>Gets the method the path ends in.</summary>
    public string? Method { get; init; }

    /// <summary>Gets the kind of owner, a name in the DataOptionOwnerKinds collection.</summary>
    public string? OwnerKind { get; init; }

    /// <summary>Gets the owning organization or workspace.</summary>
    public Guid? OwnerId { get; init; }

    /// <summary>Gets the custodian.</summary>
    public Guid? CustodianUserId { get; init; }

    /// <summary>Gets the permission a person other than the custodian must hold to change the rule.</summary>
    public string? RequiredPermission { get; init; }

    /// <summary>Gets whether the rule starts switched on.</summary>
    public bool? Active { get; init; }

    /// <summary>Gets the rule's values.</summary>
    public IReadOnlyList<NewDataConfigurationRuleValue>? Values { get; init; }
}
