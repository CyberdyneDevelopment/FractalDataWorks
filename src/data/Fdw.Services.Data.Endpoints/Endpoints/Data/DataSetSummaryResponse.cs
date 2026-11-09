using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Summary DTO for a data set, used in list responses.
/// </summary>
public class DataSetSummaryResponse : ResourceSummary
{
    /// <summary>Gets or sets the data set identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the short abbreviation for compact UI contexts.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the data set description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the data set's data classification level; <c>NotYetClassified</c> until someone picks a level.
    /// </summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the name of the category the data set sits under; null when it sits under none.</summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the data set's status, read off the latest runs of the pipelines that write it: the name of an option of <c>DataSetStatuses</c> (Unwritten, Failed, Running, Warning, Current or Idle,
    /// or one a developer added). Null for a variation, which holds no data of its own (FDW-4488).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the display names from the top of the category tree down to the data set's category, joined by " / "; null when it sits under none.</summary>
    public string? CategoryPath { get; set; }

    /// <summary>
    /// Gets or sets the kind of data set: a registered <c>DataSetTypes</c> member ("Simple", "Compound", "Federated" or "Variation").
    /// </summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets what kind of variation this data set is: "Scoped" (a workspace owns it) or "DataView" (a user does); null for a data set that is not a variation.</summary>
    public string? VariationKind { get; set; }

    /// <summary>Gets or sets the logical id of the root data set a variation varies; null for a data set that is not a variation.</summary>
    public Guid? BaseDataSetId { get; set; }

    /// <summary>Gets or sets the logical id of the view a variation narrows, or null when it varies the data set itself or is not a variation.</summary>
    public Guid? ParentVariationId { get; set; }

    /// <summary>Gets or sets the logical id of the workspace that owns a Scoped variation; null otherwise.</summary>
    public Guid? OwnerWorkspaceId { get; set; }

    /// <summary>
    /// Gets or sets the logical id of the user who owns the data set: its creator, or the owner of a data view. Null when it has no owner (made before owners existed): the list shows it as Unowned.
    /// </summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Gets or sets a variation's stored filter tree, in the body shape of the data route's filter; null when it has none or is not a variation.</summary>
    public string? VariationFilters { get; set; }

    /// <summary>Gets or sets the data set version.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Gets or sets the user who created the record.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who last modified the record.</summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreatedOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifiedOnBehalfOf { get; set; } = string.Empty;
}
