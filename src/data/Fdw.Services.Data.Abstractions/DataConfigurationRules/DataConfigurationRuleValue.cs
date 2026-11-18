using System;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>One value of a data configuration rule (FDW-6345): a literal, or the name of a parameter a run supplies.</summary>
public sealed class DataConfigurationRuleValue
{
    /// <summary>Gets the value's durable id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the value's kind, a name in the DataOptionValueKinds collection.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the literal; stated for the Literal kind and null for the Parameter kind.</summary>
    public string? Value { get; init; }

    /// <summary>Gets the parameter's name; stated for the Parameter kind and null for the Literal kind.</summary>
    public string? Parameter { get; init; }

    /// <summary>Gets the optional name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional abbreviation.</summary>
    public string? Abbreviation { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the value's place among the rule's values, from 1; for Between, 1 is the low value and 2 the high.</summary>
    public required int Ordinal { get; init; }
}
