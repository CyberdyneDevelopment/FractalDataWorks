using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>The kinds of subject a threshold can be attached to (FDW-3936): one link table each, all with the same columns.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ThresholdSubjectKindBase), typeof(IThresholdSubjectKind), typeof(ThresholdSubjectKinds))]
public abstract partial class ThresholdSubjectKinds : TypeCollectionBase<ThresholdSubjectKindBase, IThresholdSubjectKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
