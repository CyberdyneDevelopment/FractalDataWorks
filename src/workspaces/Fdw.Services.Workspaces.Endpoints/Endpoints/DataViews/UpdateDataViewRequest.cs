namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Changes a data view: <c>PATCH /data-views/{Name}</c>.</summary>
/// <remarks>
/// A null field leaves the view's value alone, which is what PATCH means; a blank string clears <c>DisplayName</c>,
/// <c>Description</c> and <c>Filters</c>. The data set a view reads and the view it is a view of are not here: a view keeps both for its life.
/// </remarks>
public class UpdateDataViewRequest
{
    /// <summary>Gets or sets the name of the view to change. It comes from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the new display name, a blank string to clear it, or null to leave it.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the new description, a blank string to clear it, or null to leave it.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new encoding, or null to leave it.</summary>
    public string? Encoding { get; set; }

    /// <summary>Gets or sets the new stored filter state, an empty string to clear it, or null to leave it.</summary>
    public string? Filters { get; set; }

    /// <summary>Gets or sets the new chart kind, or null to leave it.</summary>
    public string? ChartType { get; set; }
}
