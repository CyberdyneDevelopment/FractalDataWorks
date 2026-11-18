namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>A kind of recorded change to a data configuration rule or its values (FDW-6571): the name the service records it under, how a console shows it, and the member it changes, if it changes one.</summary>
/// <param name="Name">The change kind as the administrative audit records it, such as <c>NameChanged</c>.</param>
/// <param name="DisplayName">The name a console shows for the kind.</param>
/// <param name="MemberOf">What the changed member belongs to (<see cref="DataConfigurationRuleCatalog.RuleOwner"/> or <see cref="DataConfigurationRuleCatalog.ValueOwner"/>), or <see langword="null"/> when the kind changes no one member (a rule added, removed or restored).</param>
/// <param name="Member">The name of the changed member, or <see langword="null"/> when the kind changes no one member.</param>
public sealed record DataConfigurationRuleChangeKind(string Name, string DisplayName, string? MemberOf, string? Member);
