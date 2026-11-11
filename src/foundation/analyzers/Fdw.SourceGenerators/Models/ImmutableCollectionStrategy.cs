using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Models;

/// <summary>Values are fixed when the collection is built: a FrozenDictionary, readonly, with no Register.</summary>
[TypeOption(typeof(CollectionStrategies), "Immutable")]
[ExcludeFromCodeCoverage]
public sealed class ImmutableCollectionStrategy : CollectionStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="ImmutableCollectionStrategy"/> class.</summary>
    public ImmutableCollectionStrategy() : base(1, "Immutable") { }

    /// <inheritdoc />
    public override string RequiredNamespace => "System.Collections.Frozen";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool AllowsRegistration => false;

    /// <inheritdoc />
    public override ICollectionGenerationMode GenerationMode => CollectionGenerationModes.InstanceCollection;

    /// <inheritdoc />
    public override string DictionaryType(string keyType, string valueType) => $"FrozenDictionary<{keyType}, {valueType}>";

    /// <inheritdoc />
    public override string AllFromDictionary(string keyType, string returnType) => "_all = dictionary.ToFrozenDictionary();";

    /// <inheritdoc />
    public override string EmptyAll(string keyType, string returnType) => "_all = System.Collections.Frozen.FrozenDictionary<" + keyType + ", " + returnType + ">.Empty;";

    /// <inheritdoc />
    public override string LookupFromAll(string dictionaryName, string propertyName, string propertyType, string returnType) => $"{dictionaryName} = _all.Values.ToFrozenDictionary(x => x.{propertyName});";
}
