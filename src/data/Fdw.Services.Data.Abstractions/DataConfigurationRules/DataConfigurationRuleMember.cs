namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>A changeable member of a data configuration rule or of one of its values (FDW-6571): the name a change's <c>clear</c> and the history use, how a console shows it, and whether a change may clear it.</summary>
/// <param name="Name">The member's payload name, such as <c>description</c>.</param>
/// <param name="DisplayName">The name a console shows for the member.</param>
/// <param name="Of">What the member belongs to: <see cref="DataConfigurationRuleCatalog.RuleOwner"/> or <see cref="DataConfigurationRuleCatalog.ValueOwner"/>.</param>
/// <param name="Clearable">Whether a change may clear the member.</param>
public sealed record DataConfigurationRuleMember(string Name, string DisplayName, string Of, bool Clearable);
