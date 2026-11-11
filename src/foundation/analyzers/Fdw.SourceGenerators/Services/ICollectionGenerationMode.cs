using Fdw.Collections;

namespace Fdw.SourceGenerators.Services;

/// <summary>One value of <see cref="CollectionGenerationModes"/>: how a generated collection is shaped.</summary>
public interface ICollectionGenerationMode : ITypeOption<int, CollectionGenerationModeBase> { }
