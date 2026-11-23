using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>The field is less than or equal to the value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordOrderings), "LessOrEqual")]
public sealed class LessOrEqualRecordOrdering : RecordOrderingBase
{
    /// <summary>Initializes a new instance of the <see cref="LessOrEqualRecordOrdering"/> class.</summary>
    public LessOrEqualRecordOrdering()
        : base("LessOrEqual", RecordPredicateKeywords.LessOrEqual)
    {
    }

    /// <inheritdoc />
    public override bool Holds(int order) => order <= 0;
}
