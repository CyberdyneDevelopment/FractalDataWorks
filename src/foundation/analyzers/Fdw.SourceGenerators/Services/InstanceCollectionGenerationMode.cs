using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.SourceGenerators.Services;

/// <summary>An instance collection: values are held by an instance of the collection.</summary>
[TypeOption(typeof(CollectionGenerationModes), "InstanceCollection")]
[ExcludeFromCodeCoverage]
public sealed class InstanceCollectionGenerationMode : CollectionGenerationModeBase
{
    /// <summary>Initializes a new instance of the <see cref="InstanceCollectionGenerationMode"/> class.</summary>
    public InstanceCollectionGenerationMode() : base(2, "InstanceCollection") { }
}
