using System;
using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Data transfer object representing a glossary term.</summary>
public class GlossaryTermResponse
{
    /// <summary>Gets or sets the unique identifier of the glossary term.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the glossary term name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the term name as an alias for <see cref="Name"/> for client compatibility.</summary>
    public string Term
    {
        get => Name;
        set { if (string.IsNullOrEmpty(Name)) Name = value; }
    }

    /// <summary>Gets or sets the definition of the glossary term.</summary>
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