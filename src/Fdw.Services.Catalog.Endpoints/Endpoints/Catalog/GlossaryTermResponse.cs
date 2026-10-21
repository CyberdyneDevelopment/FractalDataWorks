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
    public string? Category { get; set; }

    /// <summary>Gets or sets the display names from the top of the Glossary category tree down to the term's category, joined by " / "; null when it sits under none.</summary>
    public string? CategoryPath { get; set; }

    /// <summary>Gets or sets the owner of the glossary term.</summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets what the term's name stands for (FDW-1682). A term that states it is an acronym; null for a term that is not one.
    /// </summary>
    public string? StandsFor { get; set; }

    /// <summary>
    /// Gets or sets whether an acronym is approved (FDW-1682). An acronym states it; null for a term that is not an acronym.
    /// </summary>
    public bool? IsApproved { get; set; }

    /// <summary>Gets or sets the names of related DataSets.</summary>
    public IList<string> RelatedDataSets { get; set; } = [];

    /// <summary>Gets or sets the date and time the glossary term was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the date and time the glossary term was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }
}