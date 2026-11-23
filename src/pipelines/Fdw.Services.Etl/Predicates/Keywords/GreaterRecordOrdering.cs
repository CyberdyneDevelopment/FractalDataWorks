using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>The field is greater than the value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordOrderings), "Greater")]
public sealed class GreaterRecordOrdering : RecordOrderingBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterRecordOrdering"/> class.</summary>
    public GreaterRecordOrdering()
        : base("Greater", RecordPredicateKeywords.Greater)
    {
    }

    /// <inheritdoc />
    public override bool Holds(int order) => order > 0;
}
