using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The part a column plays in a load: Key, Compare, Write or SequenceOrder (FDW-1108).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineLoadColumnRoleBase), typeof(IPipelineLoadColumnRole), typeof(PipelineLoadColumnRoles))]
public abstract partial class PipelineLoadColumnRoles : TypeCollectionBase<PipelineLoadColumnRoleBase, IPipelineLoadColumnRole>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
