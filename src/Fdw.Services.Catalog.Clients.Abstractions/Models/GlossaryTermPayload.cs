using System;
using System.Collections.Generic;

namespace Fdw.Services.Catalog.Clients.Models;

/// <summary>
/// Glossary term definition.
/// </summary>
public sealed class GlossaryTermPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the term name.</summary>
    public string Term { get; set; } = string.Empty;
    /// <summary>Gets or sets the term definition.</summary>
    public string Definition { get; set; } = string.Empty;
    /// <summary>Gets or sets the category the glossary term belongs to.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets the owner of the glossary term.</summary>
    public string? Owner { get; set; }
    /// <summary>Gets or sets the names of related DataSets.</summary>
    public IList<string> RelatedDataSets { get; set; } = [];
    /// <summary>Gets or sets the date and time the glossary term was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the date and time the glossary term was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }
}
