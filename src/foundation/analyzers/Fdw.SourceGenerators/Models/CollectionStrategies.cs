using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.SourceGenerators.Models;

/// <summary>TypeCollection of the dictionaries a generated collection can keep its values in.</summary>
[TypeCollection(typeof(CollectionStrategyBase), typeof(ICollectionStrategy), typeof(CollectionStrategies))]
[ExcludeFromCodeCoverage]
public abstract partial class CollectionStrategies : TypeCollectionBase<CollectionStrategyBase, ICollectionStrategy> { }
