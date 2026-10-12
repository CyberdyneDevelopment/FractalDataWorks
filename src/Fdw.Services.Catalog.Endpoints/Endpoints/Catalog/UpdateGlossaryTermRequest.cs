using System;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request to update an existing glossary term. Every property omitted leaves it unchanged.</summary>
public class UpdateGlossaryTermRequest
{
    /// <summary>Gets or sets the glossary term ID, bound from the route.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the glossary term name. Omitted leaves it unchanged.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the definition of the glossary term. Omitted leaves it unchanged.</summary>
    public string? Definition { get; set; }

    /// <summary>Gets or sets the category the glossary term belongs to. Omitted leaves it unchanged.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the owner of the glossary term. Omitted leaves it unchanged.</summary>
    public string? Owner { get; set; }
}
