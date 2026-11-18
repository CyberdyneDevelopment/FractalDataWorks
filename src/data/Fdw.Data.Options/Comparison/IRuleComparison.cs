using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>A comparison kind (FDW-6340): applies one condition to a filter and gives the filter back.</summary>
public interface IRuleComparison : ITypeOption<int, RuleComparisonBase>, IRuleKind<FilterExpression>
{
    /// <summary>Gets the kind's name. Stated here once, because the type option and the rule layer both carry it and the collection generator reads it on this interface.</summary>
    new string Name { get; }
}
