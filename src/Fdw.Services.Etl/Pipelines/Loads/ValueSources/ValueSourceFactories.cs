using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Pipelines.Loads.ValueSources;

/// <summary>
/// Builds the command-layer value source for each <c>ValueSourceKinds</c> kind: Column, Constant, CurrentTimestamp and
/// NextSequenceValue (FDW-1109).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ValueSourceFactoryBase), typeof(IValueSourceFactory), typeof(ValueSourceFactories))]
public abstract partial class ValueSourceFactories : TypeCollectionBase<ValueSourceFactoryBase, IValueSourceFactory>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
