using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for business glossary terms.
/// Stored in catalog.GlossaryTerm table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Catalog",
    ServiceType = "GlossaryTerm")]
public sealed partial class GlossaryTermImplementationConfiguration
    : IGlossaryTermImplementationConfiguration
{
    /// <summary>The domain record's durable id.</summary>
    public Guid GlossaryTermId { get; set; }


    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the unique identifier for this term.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the glossary term.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business definition of the term.
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional calculation formula for the term.
    /// </summary>
    public string? Formula { get; set; }

    /// <summary>
    /// Gets or sets the category this term belongs to.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business owner of this term.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data steward responsible for this term.
    /// </summary>
    public string Steward { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related terms for this glossary term.
    /// </summary>
    public IList<GlossaryTermRelationConfiguration> RelatedTerms { get; set; } = [];

    /// <summary>
    /// Gets or sets the linked data sets for this glossary term.
    /// </summary>
    public IList<GlossaryTermLinkedDataSetConfiguration> LinkedDataSets { get; set; } = [];
}
