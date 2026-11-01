using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A data set variation as its domain row holds it: the four properties every domain has, and everything a list of
/// variations shows, whatever the kind.
/// </summary>
/// <remarks>
/// <para>
/// A variation is a view of a data set. It holds no fields of its own: the data route runs the data set it points at.
/// It is its own domain, not a data set, so it never appears in a list or picker of data sets (FDW-1022). The kind is
/// the implementation (FDW-1026): <see cref="ScopedDataSetVariationConfiguration"/>, a data set a workspace owns as its
/// own view of a main one, and <see cref="DataViewDataSetVariationConfiguration"/>, a saved chart owned by a user.
/// <see cref="BaseDataSetId"/> is the root data set for every kind, so one query by it lists every variation of a data
/// set.
/// </para>
/// <para>
/// Maps to <c>data.DataSetVariation</c>. <see cref="DomainConfiguration.Name"/> is the unique key per tenant, because
/// the domain provider identifies a member by name alone: a Scoped variation's is the owning workspace's name and
/// <see cref="VariationName"/> joined by <c>/</c>, a data view's is its <see cref="VariationName"/>. Set once at
/// create. Exactly one of <see cref="OwnerWorkspaceId"/> and <see cref="OwnerUserId"/> is set.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class DataSetVariationDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the name the owning workspace knows the variation by: unique within that workspace.</summary>
    public string VariationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-facing display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the variation's description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the logical id of the workspace that owns a Scoped variation; null for a data view.</summary>
    public Guid? OwnerWorkspaceId { get; set; }

    /// <summary>Gets or sets the logical id of the user that owns a data view; null for a Scoped variation.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Gets or sets the logical id of the root data set the variation varies.</summary>
    public Guid BaseDataSetId { get; set; }

    /// <summary>
    /// Gets or sets the logical id of this view's parent view, or null when its parent is the data set itself (FDW-1032).
    /// <see cref="BaseDataSetId"/> is the root data set either way. Nothing changes it after the view is created (a create
    /// sets it, an update does not carry it), so a view cannot be its own ancestor. It cites the parent's logical id with no
    /// foreign key, as every citation does, because the domain row is re-versioned when a listed field changes and a
    /// foreign key needs an id that is unique across versions.
    /// </summary>
    public Guid? ParentVariationId { get; set; }

    /// <summary>
    /// Gets or sets the view's stored filter tree, in the body shape of the data route's filter, or null for none. The data
    /// route adds the filter of every view in the chain, parent first, to the request's own (FDW-1032).
    /// </summary>
    public string? Filters { get; set; }

    /// <summary>
    /// Gets or sets how sensitive this variation's data is: the <c>ClassificationId</c> of a <c>DataClassifications</c>
    /// level, its own and not its base data set's. <c>NotYetClassified</c> until someone picks one; never empty (FDW-1027).
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets when the variation was created: carried across versions of the row.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
