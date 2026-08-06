namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// Request to update a glossary term.
/// </summary>
public sealed class UpdateGlossaryTermRequest
{
    /// <summary>Gets or sets the term name.</summary>
    public string Term { get; set; } = string.Empty;
    /// <summary>Gets or sets the term definition.</summary>
    public string Definition { get; set; } = string.Empty;
}
