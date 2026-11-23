using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>The field's text ends with the word.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordTextMatches), "EndsWith")]
public sealed class EndsWithRecordTextMatch : RecordTextMatchBase
{
    /// <summary>Initializes a new instance of the <see cref="EndsWithRecordTextMatch"/> class.</summary>
    public EndsWithRecordTextMatch()
        : base("EndsWith", RecordPredicateKeywords.EndsWith)
    {
    }

    /// <inheritdoc />
    public override bool Matches(string actual, string text) => actual.EndsWith(text, StringComparison.OrdinalIgnoreCase);
}
