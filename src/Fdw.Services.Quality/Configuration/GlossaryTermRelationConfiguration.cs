using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for a glossary term relation.
/// Child of GlossaryTermConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Catalog")]
public sealed partial class GlossaryTermRelationConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related term name.
    /// </summary>
    public string RelatedTermName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relation type (Synonym, Antonym, Related).
    /// </summary>
    public string RelationType { get; set; } = "Related";
}
