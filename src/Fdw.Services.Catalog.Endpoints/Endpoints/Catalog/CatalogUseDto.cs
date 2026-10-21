using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>One thing that uses, or is used by, the thing asked about (FDW-1683).</summary>
public sealed class CatalogUseDto
{
    /// <summary>Gets or sets what is done: one of <see cref="CatalogRelations"/>, read from the pipeline, calculation or term towards the other.</summary>
    public string Relation { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of the other thing: one of <see cref="CatalogKinds"/>.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the other thing.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the names the other thing sits under, outermost first; empty when it sits under nothing.</summary>
    public IList<string> Parents { get; set; } = [];
}
