using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The part of a transform step request that the ordering transforms (Sort and LatestBy) read (FDW-1109). A request that
/// can carry those transforms implements this beside <see cref="ITransformOperationSpec"/>; the key fields of LatestBy and
/// Distinct arrive in <see cref="ITransformOperationSpec.GroupByFields"/>.
/// </summary>
/// <remarks>
/// Why a second interface and not more members on <see cref="ITransformOperationSpec"/>: this assembly targets
/// netstandard2.0, which cannot give an interface member a default body, so adding members there would break every
/// existing implementer at once. A step that needs these members and whose request does not implement this interface is
/// refused, never read as empty.
/// </remarks>
public interface ITransformOrderingSpec
{
    /// <summary>Gets the sort fields (for Sort transforms), most significant first.</summary>
    IReadOnlyList<ISortFieldSpec> SortFields { get; }

    /// <summary>Gets the order field (for LatestBy transforms): the field whose value picks the row to keep.</summary>
    string? OrderField { get; }

    /// <summary>Gets the keep rule name (for LatestBy transforms), resolved against <c>KeepRules</c>: First or Last.</summary>
    string? KeepRule { get; }
}
