using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>The field is greater than or equal to the value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordOrderings), "GreaterOrEqual")]
public sealed class GreaterOrEqualRecordOrdering : RecordOrderingBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterOrEqualRecordOrdering"/> class.</summary>
    public GreaterOrEqualRecordOrdering()
        : base("GreaterOrEqual", RecordPredicateKeywords.GreaterOrEqual)
    {
    }

    /// <inheritdoc />
    public override bool Holds(int order) => order >= 0;
}
