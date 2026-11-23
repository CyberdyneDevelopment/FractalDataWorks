using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>Base class for the ways a record predicate compares a field's text with a word.</summary>
public abstract class RecordTextMatchBase : TypeOptionBase<RecordTextMatchBase>, IRecordTextMatch
{
    /// <summary>Initializes a new instance of the <see cref="RecordTextMatchBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="word">The word a predicate states the comparison by.</param>
    protected RecordTextMatchBase(string name, string word)
        : base(name)
    {
        Word = word;
    }

    /// <inheritdoc />
    [TypeLookup("ByWord")]
    public string Word { get; }

    /// <inheritdoc />
    public abstract bool Matches(string actual, string text);
}
