using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Models;

/// <summary>Values can be added after the collection is built from several threads: a ConcurrentDictionary with Register.</summary>
[TypeOption(typeof(CollectionStrategies), "Mutable")]
[ExcludeFromCodeCoverage]
public sealed class MutableCollectionStrategy : CollectionStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="MutableCollectionStrategy"/> class.</summary>
    public MutableCollectionStrategy() : base(2, "Mutable") { }

    /// <inheritdoc />
    public override string RequiredNamespace => "System.Collections.Concurrent";

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool AllowsRegistration => true;

    /// <inheritdoc />
    public override ICollectionGenerationMode GenerationMode => CollectionGenerationModes.InstanceCollection;

    /// <inheritdoc />
    public override string DictionaryType(string keyType, string valueType) => $"ConcurrentDictionary<{keyType}, {valueType}>";

    /// <inheritdoc />
    public override string AllFromDictionary(string keyType, string returnType) => "_all = new System.Collections.Concurrent.ConcurrentDictionary<" + keyType + ", " + returnType + ">(dictionary);";

    /// <inheritdoc />
    public override string EmptyAll(string keyType, string returnType) => "_all = new System.Collections.Concurrent.ConcurrentDictionary<" + keyType + ", " + returnType + ">();";

    /// <inheritdoc />
    public override string LookupFromAll(string dictionaryName, string propertyName, string propertyType, string returnType) => $"{dictionaryName} = new System.Collections.Concurrent.ConcurrentDictionary<{propertyType}, {returnType}>(_all.Values.ToDictionary(x => x.{propertyName}));";
}
