using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>The field's text contains the word.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordTextMatches), "ContainsText")]
public sealed class ContainsRecordTextMatch : RecordTextMatchBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainsRecordTextMatch"/> class.</summary>
    public ContainsRecordTextMatch()
        : base("ContainsText", RecordPredicateKeywords.Contains)
    {
    }

    /// <inheritdoc />
    public override bool Matches(string actual, string text) => actual.Contains(text, StringComparison.OrdinalIgnoreCase);
}
