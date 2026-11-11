using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Models;

/// <summary>Values are created on request: a plain Dictionary with Register.</summary>
[TypeOption(typeof(CollectionStrategies), "Factory")]
[ExcludeFromCodeCoverage]
public sealed class FactoryCollectionStrategy : CollectionStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="FactoryCollectionStrategy"/> class.</summary>
    public FactoryCollectionStrategy() : base(3, "Factory") { }

    /// <inheritdoc />
    public override string RequiredNamespace => string.Empty;

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool AllowsRegistration => true;

    /// <inheritdoc />
    public override ICollectionGenerationMode GenerationMode => CollectionGenerationModes.FactoryCollection;

    /// <inheritdoc />
    public override string DictionaryType(string keyType, string valueType) => $"Dictionary<{keyType}, {valueType}>";

    /// <inheritdoc />
    public override string AllFromDictionary(string keyType, string returnType) => "_all = dictionary;";

    /// <inheritdoc />
    public override string EmptyAll(string keyType, string returnType) => "_all = new System.Collections.Generic.Dictionary<" + keyType + ", " + returnType + ">();";

    /// <inheritdoc />
    public override string LookupFromAll(string dictionaryName, string propertyName, string propertyType, string returnType) => $"{dictionaryName} = _all.Values.ToDictionary(x => x.{propertyName});";
}
