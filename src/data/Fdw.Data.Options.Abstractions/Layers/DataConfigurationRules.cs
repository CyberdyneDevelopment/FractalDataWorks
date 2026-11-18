using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>
/// The root of the data configuration rule paths (FDW-6340): <c>DataConfigurationRules:level:...:Kind.Method</c>. It names no family:
/// its options are whatever child collections are registered under it.
/// </summary>
[TypeCollection(typeof(RuleLayerBase), typeof(IRuleLayerOption), typeof(DataConfigurationRules))]
public abstract partial class DataConfigurationRules : TypeCollectionBase<RuleLayerBase, IRuleLayerOption>
{
}
