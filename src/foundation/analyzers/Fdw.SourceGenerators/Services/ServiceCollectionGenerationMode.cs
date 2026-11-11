using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.SourceGenerators.Services;

/// <summary>A service collection: values are registered with dependency injection.</summary>
[TypeOption(typeof(CollectionGenerationModes), "ServiceCollection")]
[ExcludeFromCodeCoverage]
public sealed class ServiceCollectionGenerationMode : CollectionGenerationModeBase
{
    /// <summary>Initializes a new instance of the <see cref="ServiceCollectionGenerationMode"/> class.</summary>
    public ServiceCollectionGenerationMode() : base(4, "ServiceCollection") { }
}
