namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>A value to add to a rule (FDW-6345). Nothing has a default: a field a value needs and this leaves out is refused with its own code.</summary>
public sealed class NewDataConfigurationRuleValue
{
    /// <summary>Gets the kind, a name in the DataOptionValueKinds collection.</summary>
    public string? Kind { get; init; }

    /// <summary>Gets the literal.</summary>
    public string? Value { get; init; }

    /// <summary>Gets the parameter's name.</summary>
    public string? Parameter { get; init; }

    /// <summary>Gets the optional name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the optional abbreviation.</summary>
    public string? Abbreviation { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the value's place among the rule's values, from 1.</summary>
    public int? Ordinal { get; init; }
}
