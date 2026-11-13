namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// An optional capability of a data set kind (FDW-5300): the kind builds its whole set and applies a query's filter, ordering and page to the
/// rows it holds, so a page asked for is the page returned though no store pages it.
/// </summary>
/// <remarks>
/// <para>
/// Why a separate interface and not a member of <see cref="IDataSetType"/>: a new member of a public interface breaks every outside implementer, and
/// the target of this project has no default interface members. A kind that does not state the capability is unchanged.
/// </para>
/// <para>
/// What it changes: the row routes (<c>DataSetReadGate.Readable</c>) answer a paged query from such a kind, where they refuse a kind whose
/// <see cref="IDataSetType.PagesInStore"/> is false and that says nothing else. What it does not change: <see cref="IDataSetType.PagesInStore"/> stays
/// false, so a read that makes the kind a source of another read takes it whole and never in pages, which would run its whole set once per page.
/// </para>
/// </remarks>
public interface IShapesItsOwnRead
{
    /// <summary>Gets whether the kind applies a query's filter, ordering and page to the whole set it holds.</summary>
    bool ShapesItsOwnRead { get; }
}
