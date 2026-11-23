using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>Reads what a rule criteria names (FDW-3936).</summary>
public static class ThresholdCriteria
{
    /// <summary>Lists the fields a criteria names, each once, in the order it first names them.</summary>
    /// <param name="criteria">The criteria: conditions in AND/OR groups.</param>
    /// <returns>The field names.</returns>
    public static IReadOnlyList<string> FieldsNamed(IFilterNode criteria) =>
        [.. FilterNodeKinds.Conditions(criteria).Select(condition => condition.PropertyName).Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>Lists the conditions of a criteria, in the order they are stated.</summary>
    /// <param name="criteria">The criteria: conditions in AND/OR groups.</param>
    /// <returns>Every condition, at any depth.</returns>
    public static IReadOnlyList<FilterCondition> ConditionsOf(IFilterNode criteria) =>
        [.. FilterNodeKinds.Conditions(criteria).Cast<FilterCondition>()];
}
