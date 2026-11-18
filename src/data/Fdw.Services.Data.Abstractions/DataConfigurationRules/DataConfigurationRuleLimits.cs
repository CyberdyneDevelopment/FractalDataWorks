using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>
/// The longest text each field of a rule and of a value takes (FDW-6575), as the two rule tables declare it. A field whose container declares no length is not in the answer: nothing is
/// invented for it.
/// </summary>
public sealed class DataConfigurationRuleLimits
{
    /// <summary>Gets each rule field's declared maximum length, by the member's payload name.</summary>
    public required IReadOnlyDictionary<string, int> Rule { get; init; }

    /// <summary>Gets each value field's declared maximum length, by the member's payload name.</summary>
    public required IReadOnlyDictionary<string, int> Value { get; init; }
}
