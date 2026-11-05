using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// The kinds of threshold (FDW-3936). A story that has a subject adds its option here: a pipeline run, a component input and output, a connection, the waiting runs of a
/// schedule, the quality of a data set.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ThresholdTypeBase), typeof(IThreshold), typeof(ThresholdTypes))]
public abstract partial class ThresholdTypes : TypeCollectionBase<ThresholdTypeBase, IThreshold>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
