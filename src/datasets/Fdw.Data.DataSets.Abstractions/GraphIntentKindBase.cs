using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Base for the kinds of intended source of a Graph data set.</summary>
/// <remarks>
/// No id is passed: <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully qualified type name. Nothing persists
/// that id; <c>data.GraphDataSetIntent.IntentKind</c> holds the option's name.
/// </remarks>
public abstract class GraphIntentKindBase : TypeOptionBase<GraphIntentKindBase>, IGraphIntentKind
{
    /// <summary>Initializes a new instance of the <see cref="GraphIntentKindBase"/> class.</summary>
    /// <remarks>Used only by the source generator's Empty sentinel; a real option always supplies its parameters.</remarks>
    protected GraphIntentKindBase()
        : this(string.Empty, false, false)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GraphIntentKindBase"/> class.</summary>
    /// <param name="name">The kind name, stored in <c>data.GraphDataSetIntent.IntentKind</c>.</param>
    /// <param name="namesDataStore">Whether an intent of this kind names a data store.</param>
    /// <param name="namesDataSet">Whether an intent of this kind names another data set.</param>
    protected GraphIntentKindBase(string name, bool namesDataStore, bool namesDataSet)
        : base(name)
    {
        NamesDataStore = namesDataStore;
        NamesDataSet = namesDataSet;
    }

    /// <inheritdoc />
    public bool NamesDataStore { get; }

    /// <inheritdoc />
    public bool NamesDataSet { get; }
}
