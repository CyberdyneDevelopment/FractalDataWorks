namespace Fdw.Data.Components.DataStores;

/// <summary>
/// Mutable form model for the DataStore editor wizard.
/// Bound to form controls across steps 0–2.
/// </summary>
public sealed class DataStoreEditorModel
{
    /// <summary>Gets or sets the data store name (immutable in edit mode).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the selected connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected connection type name (resolved from the selected connection).</summary>
    public string ConnectionTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected store type (from DataStoreTypes TypeCollection).</summary>
    public string StoreType { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected write mode (from capabilities.WriteModes).</summary>
    public string? WriteMode { get; set; }

    /// <summary>Gets or sets whether the data store is active.</summary>
    public bool IsActive { get; set; } = true;
}
