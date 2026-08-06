namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// Request to create a glossary term.
/// </summary>
public sealed class CreateGlossaryTermRequest
{
    /// <summary>Gets or sets the term name.</summary>
    public string Term { get; set; } = string.Empty;
    /// <summary>Gets or sets the term definition.</summary>
    public string Definition { get; set; } = string.Empty;
    /// <summary>Gets or sets the category the glossary term belongs to.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets the owner of the glossary term.</summary>
    public string? Owner { get; set; }
}
