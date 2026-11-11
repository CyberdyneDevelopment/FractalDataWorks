using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.SourceGenerators.Services;

/// <summary>A static collection: values are created once and exposed through static members.</summary>
[TypeOption(typeof(CollectionGenerationModes), "StaticCollection")]
[ExcludeFromCodeCoverage]
public sealed class StaticCollectionGenerationMode : CollectionGenerationModeBase
{
    /// <summary>Initializes a new instance of the <see cref="StaticCollectionGenerationMode"/> class.</summary>
    public StaticCollectionGenerationMode() : base(1, "StaticCollection") { }
}
