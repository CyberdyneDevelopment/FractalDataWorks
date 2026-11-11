using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.SourceGenerators.Services;

/// <summary>TypeCollection of the shapes a generated collection can take.</summary>
[TypeCollection(typeof(CollectionGenerationModeBase), typeof(ICollectionGenerationMode), typeof(CollectionGenerationModes))]
[ExcludeFromCodeCoverage]
public abstract partial class CollectionGenerationModes : TypeCollectionBase<CollectionGenerationModeBase, ICollectionGenerationMode> { }
