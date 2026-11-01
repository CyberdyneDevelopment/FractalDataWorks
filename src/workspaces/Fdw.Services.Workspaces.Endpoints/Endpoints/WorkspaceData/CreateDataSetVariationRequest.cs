using Fdw.Data.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Creates a data set a workspace owns as a variation of a main data set.</summary>
public class CreateDataSetVariationRequest
{
    /// <summary>Gets or sets the name of the workspace that will own the variation.</summary>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variation's name. A data set is looked up by name alone, so it is unique across
    /// the tenant's data sets, main and variation alike.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the main data set it varies. Needed when the view has no parent view; when it has one, the data
    /// set is the parent's, and this may be left out or must name that same data set (FDW-1032).
    /// </summary>
    public string BaseDataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of a view of this workspace that this view is a view of, or null when its parent is the data set.
    /// The parent never changes once the view exists.
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>
    /// Gets or sets the view's stored filter tree, in the body shape of the data route's filter, or null for none. It is
    /// checked against the data set's fields before anything is saved.
    /// </summary>
    public string? Filters { get; set; }

    /// <summary>Gets or sets the variation's display name, when it has one.</summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the variation's own data classification: a <c>ClassificationId</c> of <c>DataClassifications</c>.
    /// <c>NotYetClassified</c> until someone picks one (FDW-1027).
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;
}
