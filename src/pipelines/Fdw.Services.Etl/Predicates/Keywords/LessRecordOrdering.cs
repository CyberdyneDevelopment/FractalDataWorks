using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>The field is less than the value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordOrderings), "Less")]
public sealed class LessRecordOrdering : RecordOrderingBase
{
    /// <summary>Initializes a new instance of the <see cref="LessRecordOrdering"/> class.</summary>
    public LessRecordOrdering()
        : base("Less", RecordPredicateKeywords.Less)
    {
    }

    /// <inheritdoc />
    public override bool Holds(int order) => order < 0;
}
