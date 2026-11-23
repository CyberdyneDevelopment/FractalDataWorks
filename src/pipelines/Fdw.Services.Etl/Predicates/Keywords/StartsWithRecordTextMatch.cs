using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>The field's text starts with the word.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordTextMatches), "StartsWith")]
public sealed class StartsWithRecordTextMatch : RecordTextMatchBase
{
    /// <summary>Initializes a new instance of the <see cref="StartsWithRecordTextMatch"/> class.</summary>
    public StartsWithRecordTextMatch()
        : base("StartsWith", RecordPredicateKeywords.StartsWith)
    {
    }

    /// <inheritdoc />
    public override bool Matches(string actual, string text) => actual.StartsWith(text, StringComparison.OrdinalIgnoreCase);
}
