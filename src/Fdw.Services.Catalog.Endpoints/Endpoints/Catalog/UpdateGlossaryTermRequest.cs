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

    /// <summary>
    /// Gets or sets the name of the Glossary category the term sits under. Left out, the current category stays; an empty string takes the
    /// term out of its category. A name that is not a category of that type is refused.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the owner of the glossary term. Omitted leaves it unchanged.</summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets what the term's name stands for (FDW-1682). Left out, the stored value stays; a term is made an acronym by stating it
    /// together with <see cref="IsApproved"/>.
    /// </summary>
    public string? StandsFor { get; set; }

    /// <summary>Gets or sets whether the acronym is approved (FDW-1682). Left out, the stored value stays.</summary>
    public bool? IsApproved { get; set; }
}
