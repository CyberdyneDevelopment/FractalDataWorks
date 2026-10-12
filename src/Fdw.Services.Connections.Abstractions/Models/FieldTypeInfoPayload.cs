namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Describes a single field type supported by a connection type.
/// </summary>
public sealed class FieldTypeInfoPayload
{
    /// <summary>Gets or sets the canonical type name used in code (e.g. "varchar").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the native database type expression (e.g. "varchar(max)").</summary>
    public string DbTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable label shown in the UI (e.g. "Text (varchar)").</summary>
    public string DisplayName { get; set; } = string.Empty;
}
