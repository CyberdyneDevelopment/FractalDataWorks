using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>One value a kind of subject reports (FDW-3948): a field a rule criteria may name.</summary>
public sealed class ThresholdFieldResponse
{
    /// <summary>Gets or sets the field name, as a criteria condition names it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the type of the value: Int64, Double, TimeSpan and so on. A criteria compares it with a value of this type.</summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>Gets or sets what the value counts or measures.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Gets or sets what the field says.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the comparisons a condition on this field may use, by name (FilterOperators options): the rule is ThresholdFieldOperators.For the value type. A condition naming another is refused.</summary>
    public IReadOnlyList<string> Operators { get; set; } = [];
}
