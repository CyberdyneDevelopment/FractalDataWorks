using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.SourceGenerators.Services;

/// <summary>A factory collection: values are created on request.</summary>
[TypeOption(typeof(CollectionGenerationModes), "FactoryCollection")]
[ExcludeFromCodeCoverage]
public sealed class FactoryCollectionGenerationMode : CollectionGenerationModeBase
{
    /// <summary>Initializes a new instance of the <see cref="FactoryCollectionGenerationMode"/> class.</summary>
    public FactoryCollectionGenerationMode() : base(3, "FactoryCollection") { }
}
