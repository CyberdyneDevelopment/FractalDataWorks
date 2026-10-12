using System;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The contract every data set variation implementation carries.</summary>
/// <remarks>
/// A variation's fields are the ones its list shows, and they live on the domain row
/// (<see cref="DataSetVariationDomainConfiguration"/>); reading a variation stamps them onto its implementation. Each
/// kind's implementation row holds its key to the domain row and whatever only that kind has (FDW-1026).
/// </remarks>
public interface IDataSetVariationImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the name the owning workspace knows the variation by.</summary>
    string VariationName { get; set; }

    /// <summary>Gets or sets the human-facing display name.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the variation's description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the logical id of the workspace that owns a Scoped variation; null for a data view.</summary>
    Guid? OwnerWorkspaceId { get; set; }

    /// <summary>Gets or sets the logical id of the user that owns a data view; null for a Scoped variation.</summary>
    Guid? OwnerUserId { get; set; }

    /// <summary>Gets or sets the logical id of the root data set the variation varies.</summary>
    Guid BaseDataSetId { get; set; }

    /// <summary>Gets or sets the logical id of the parent view, or null when the parent is the data set; stamped from the domain row.</summary>
    Guid? ParentVariationId { get; set; }

    /// <summary>Gets or sets the view's stored filter tree, or null for none; stamped from the domain row.</summary>
    string? Filters { get; set; }

    /// <summary>Gets or sets the variation's own data classification: a <c>ClassificationId</c> of <c>DataClassifications</c>, stamped from the domain row.</summary>
    int ClassificationId { get; set; }

    /// <summary>Gets or sets when the variation was created: stamped from the domain row, never persisted by the implementation.</summary>
    DateTimeOffset CreatedAt { get; set; }
}
