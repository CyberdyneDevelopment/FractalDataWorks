using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>Base class for the comparison signs a record predicate states between a field and a value.</summary>
public abstract class RecordOrderingBase : TypeOptionBase<RecordOrderingBase>, IRecordOrdering
{
    /// <summary>Initializes a new instance of the <see cref="RecordOrderingBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="word">The sign a predicate states the comparison by.</param>
    protected RecordOrderingBase(string name, string word)
        : base(name)
    {
        Word = word;
    }

    /// <inheritdoc />
    [TypeLookup("ByWord")]
    public string Word { get; }

    /// <inheritdoc />
    public abstract bool Holds(int order);
}
