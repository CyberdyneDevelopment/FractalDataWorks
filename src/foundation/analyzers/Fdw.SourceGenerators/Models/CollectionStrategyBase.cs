using Fdw.Collections;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Models;

/// <summary>
/// Base of every <see cref="CollectionStrategies"/> option: the dictionary a generated collection keeps its values in, and the code that
/// builds it. Each member returns the text the generated collection is written with.
/// </summary>
public abstract class CollectionStrategyBase : TypeOptionBase<CollectionStrategyBase>, ICollectionStrategy
{
    /// <summary>Initializes a new instance of the <see cref="CollectionStrategyBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name.</param>
    protected CollectionStrategyBase(int id, string name) : base(id, name) { }

    /// <inheritdoc />
    public abstract string RequiredNamespace { get; }

    /// <inheritdoc />
    public abstract bool IsReadOnly { get; }

    /// <inheritdoc />
    public abstract bool AllowsRegistration { get; }

    /// <inheritdoc />
    public abstract ICollectionGenerationMode GenerationMode { get; }

    /// <inheritdoc />
    public abstract string DictionaryType(string keyType, string valueType);

    /// <inheritdoc />
    public abstract string AllFromDictionary(string keyType, string returnType);

    /// <inheritdoc />
    public abstract string EmptyAll(string keyType, string returnType);

    /// <inheritdoc />
    public abstract string LookupFromAll(string dictionaryName, string propertyName, string propertyType, string returnType);
}
