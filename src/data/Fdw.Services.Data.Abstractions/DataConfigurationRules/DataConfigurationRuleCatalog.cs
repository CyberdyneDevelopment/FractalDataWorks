using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>
/// The one list of what can change in a data configuration rule (FDW-6571): the changeable members of a rule and of a value, and the kinds of change the service records. The service takes its
/// clearable members from it, the history route names each recorded change through it and the limits route lists its members, so no caller holds a list of its own.
/// </summary>
public static class DataConfigurationRuleCatalog
{
    /// <summary>What a rule's own members belong to.</summary>
    public const string RuleOwner = "rule";

    /// <summary>What a value's members belong to.</summary>
    public const string ValueOwner = "value";

    /// <summary>Gets the changeable members of a rule and of a value.</summary>
    public static IReadOnlyList<DataConfigurationRuleMember> Members { get; } =
    [
        new("name", "Name", RuleOwner, false),
        new("abbreviation", "Abbreviation", RuleOwner, false),
        new("description", "Description", RuleOwner, true),
        new("column", "Column", RuleOwner, false),
        new("path", "Path", RuleOwner, false),
        new("method", "Method", RuleOwner, false),
        new("owner", "Owner", RuleOwner, false),
        new("custodian", "Custodian", RuleOwner, false),
        new("requiredPermission", "Required permission", RuleOwner, true),
        new("active", "Active", RuleOwner, false),
        new("kind", "Kind", ValueOwner, false),
        new("value", "Value", ValueOwner, false),
        new("parameter", "Parameter", ValueOwner, false),
        new("name", "Name", ValueOwner, true),
        new("abbreviation", "Abbreviation", ValueOwner, true),
        new("description", "Description", ValueOwner, true),
        new("ordinal", "Position", ValueOwner, false),
    ];

    /// <summary>Gets the kinds of change the service records, in the names the administrative audit holds.</summary>
    public static IReadOnlyList<DataConfigurationRuleChangeKind> ChangeKinds { get; } =
    [
        new("RuleAdded", "Rule added", null, null),
        new("RuleRemoved", "Rule removed", null, null),
        new("RuleRestored", "Rule restored", null, null),
        new("NameChanged", "Name changed", RuleOwner, "name"),
        new("AbbreviationChanged", "Abbreviation changed", RuleOwner, "abbreviation"),
        new("DescriptionChanged", "Description changed", RuleOwner, "description"),
        new("ColumnChanged", "Column changed", RuleOwner, "column"),
        new("PathChanged", "Path changed", RuleOwner, "path"),
        new("OwnerKindChanged", "Owner kind changed", RuleOwner, "owner"),
        new("OwnerChanged", "Owner changed", RuleOwner, "owner"),
        new("CustodianChanged", "Custodian changed", RuleOwner, "custodian"),
        new("RequiredPermissionChanged", "Required permission changed", RuleOwner, "requiredPermission"),
        new("ActiveChanged", "Switched on or off", RuleOwner, "active"),
        new("ValueAdded", "Value added", ValueOwner, "value"),
        new("ValueChanged", "Value changed", ValueOwner, "value"),
        new("ValueRemoved", "Value removed", ValueOwner, "value"),
    ];

    /// <summary>Gets the names of the members of a rule, or of a value, that a change may clear.</summary>
    /// <param name="owner"><see cref="RuleOwner"/> or <see cref="ValueOwner"/>.</param>
    /// <returns>The clearable member names.</returns>
    public static IReadOnlyList<string> Clearable(string owner) => [.. Members.Where(member => member.Clearable && string.Equals(member.Of, owner, StringComparison.Ordinal)).Select(member => member.Name)];

    /// <summary>Finds a kind of change by the name the audit holds.</summary>
    /// <param name="name">The change kind.</param>
    /// <returns>The kind; or <see langword="null"/> when the catalog does not know it.</returns>
    public static DataConfigurationRuleChangeKind? ChangeKind(string name) => ChangeKinds.FirstOrDefault(kind => string.Equals(kind.Name, name, StringComparison.Ordinal));

    /// <summary>Finds a member of a rule or of a value.</summary>
    /// <param name="owner"><see cref="RuleOwner"/> or <see cref="ValueOwner"/>.</param>
    /// <param name="name">The member's payload name.</param>
    /// <returns>The member; or <see langword="null"/> when the catalog does not know it.</returns>
    public static DataConfigurationRuleMember? Member(string owner, string name)
        => Members.FirstOrDefault(member => string.Equals(member.Of, owner, StringComparison.Ordinal) && string.Equals(member.Name, name, StringComparison.Ordinal));
}
