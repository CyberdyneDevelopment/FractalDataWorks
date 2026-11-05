using System;
using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>One value a kind of subject reports (FDW-3936): the field a rule criteria may name.</summary>
/// <param name="Name">The field name, as a criteria condition names it.</param>
/// <param name="ValueType">The type of the value the subject reports.</param>
/// <param name="Unit">What the value counts or measures, as the console shows it beside the field.</param>
/// <param name="Description">What the field says.</param>
public sealed record ThresholdField(string Name, Type ValueType, string Unit, string Description)
{
    /// <summary>Gets the comparisons the field accepts: the names of FilterOperators options, from the type of its value (FDW-4001).</summary>
    public IReadOnlyList<string> Operators => ThresholdFieldOperators.For(ValueType);
}
