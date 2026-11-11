using Fdw.Collections;

namespace Fdw.SourceGenerators.Services;

/// <summary>Base of every <see cref="CollectionGenerationModes"/> option: how a generated collection is shaped.</summary>
public abstract class CollectionGenerationModeBase : TypeOptionBase<CollectionGenerationModeBase>, ICollectionGenerationMode
{
    /// <summary>Initializes a new instance of the <see cref="CollectionGenerationModeBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name.</param>
    protected CollectionGenerationModeBase(int id, string name) : base(id, name) { }
}
