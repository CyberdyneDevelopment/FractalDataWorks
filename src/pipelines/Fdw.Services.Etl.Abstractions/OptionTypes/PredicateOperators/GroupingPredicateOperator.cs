using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Groups conditions so a connective applies to the whole group.</summary>
[TypeOption(typeof(PredicateOperators), "Grouping")]
public sealed class GroupingPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="GroupingPredicateOperator"/> class.</summary>
    public GroupingPredicateOperator() : base("Grouping", ["( )"], "Groups conditions so a connective applies to the whole group.", "Grouping", "(Region == 'West' or Region == 'East') and Amount > 100")
    {
    }
}
