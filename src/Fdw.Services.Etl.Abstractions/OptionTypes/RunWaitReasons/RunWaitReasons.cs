using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Why a pipeline run that has been accepted is not running yet (FDW-3097): the server limit, the tenant limit, the pipeline's own limit, a table
/// a run in flight writes, a run that must run alone, or a run ahead of it.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RunWaitReasonBase), typeof(IRunWaitReason), typeof(RunWaitReasons))]
public abstract partial class RunWaitReasons : TypeCollectionBase<RunWaitReasonBase, IRunWaitReason>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
