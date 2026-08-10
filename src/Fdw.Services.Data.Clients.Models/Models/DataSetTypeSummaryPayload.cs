namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Summary payload for an available DataSet type, sourced from the DataSetTypes TypeCollection.
/// </summary>
public sealed class DataSetTypeSummaryPayload
{
    /// <summary>Gets or sets the type name (e.g., "Standard", "Compound", "Federated").</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-facing display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of this DataSet type.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the category (always "Dataset" for DataSet types).</summary>
    public string Category { get; set; } = string.Empty;
}
