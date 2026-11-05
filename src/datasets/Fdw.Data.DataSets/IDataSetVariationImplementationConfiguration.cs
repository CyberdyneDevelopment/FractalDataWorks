using System;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The contract of the Variation kind of data set (FDW-3638): a data set that is a view of another data set.</summary>
/// <remarks>
/// A variation holds no fields of its own: it reads the data set it varies and applies every filter in its chain, parent
/// first (FDW-1032). Its display name, description and classification are the ones every data set carries
/// (<see cref="IDataSetImplementationConfiguration"/>); this adds who owns it, what it varies and what only some variations
/// have. A workspace owns a Scoped variation and a user owns a data view, which is also what tells the two kinds apart.
/// </remarks>
public interface IDataSetVariationImplementationConfiguration : IDataSetImplementationConfiguration
{
    /// <summary>Gets or sets the name the owning workspace knows the variation by.</summary>
    string VariationName { get; set; }

    /// <summary>Gets or sets the logical id of the workspace that owns a Scoped variation; null for a data view.</summary>
    Guid? OwnerWorkspaceId { get; set; }

    /// <summary>Gets or sets the logical id of the user that owns a data view; null for a Scoped variation.</summary>
    Guid? OwnerUserId { get; set; }

    /// <summary>Gets or sets the logical id of the root data set the variation varies.</summary>
    Guid BaseDataSetId { get; set; }

    /// <summary>Gets or sets the logical id of the parent view, or null when the parent is the data set.</summary>
    Guid? ParentVariationId { get; set; }

    /// <summary>
    /// Gets or sets the view's stored filter tree, in the body shape of the data route's filter, or null for none. It is not
    /// called Filters because a data set already has a Filters property, the conditions every kind carries.
    /// </summary>
    string? StoredFilters { get; set; }

    /// <summary>Gets or sets the JSON of what a data view's chart plots; null for a Scoped variation and for a view that draws no chart.</summary>
    string? Encoding { get; set; }

    /// <summary>Gets or sets a data view's chart type; null for a Scoped variation and for a view that draws no chart.</summary>
    string? ChartType { get; set; }

    /// <summary>Gets or sets when the variation was created.</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the tenant the variation is in: the scope it is judged at (FDW-2328).</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets what kind of variation this is: <see cref="VariationKinds.Scoped"/> when a workspace owns it, <see cref="VariationKinds.DataView"/> when a user does.</summary>
    string VariationKind { get; }
}
