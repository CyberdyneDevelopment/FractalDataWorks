using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>The ways a soft delete picks the rows it flags: <c>MarkerMatches</c> and <c>AbsentFromSource</c>.</summary>
[TypeCollection(typeof(SoftDeleteModeBase), typeof(ISoftDeleteMode), typeof(SoftDeleteModes))]
[ExcludeFromCodeCoverage]
public abstract partial class SoftDeleteModes : TypeCollectionBase<SoftDeleteModeBase, ISoftDeleteMode>
{
}
