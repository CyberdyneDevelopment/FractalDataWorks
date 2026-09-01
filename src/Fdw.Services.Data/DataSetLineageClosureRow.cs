using System;

namespace Fdw.Services.Data;

/// <summary>
/// One row of <c>data.DataSetLineageClosure</c> — an ancestor/descendant pair and the shortest number
/// of hops between them.
/// </summary>
/// <remarks>
/// Matches the view's own three columns exactly; see <c>data.DataSetLineageClosure.sql</c> for what
/// each means. Filtered on <see cref="AncestorId"/> for everything downstream of a DataSet, or on
/// <see cref="DescendantId"/> for everything upstream of one.
/// </remarks>
public sealed class DataSetLineageClosureRow
{
    /// <summary>Gets or sets the ancestor DataSet's id.</summary>
    public Guid AncestorId { get; set; }

    /// <summary>Gets or sets the descendant DataSet's id.</summary>
    public Guid DescendantId { get; set; }

    /// <summary>Gets or sets the shortest number of hops between them.</summary>
    public int Depth { get; set; }
}
