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
// Why: IGenericConfiguration is required by ImplementationConfigurationProviderBase<T>
// for dual-source (ctrl+cfg) provider pattern.
public sealed partial class GlossaryTermConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Catalogs";

    /// <inheritdoc />
    // Why: Matches ServiceCategory from [ManagedConfiguration] attribute for IOptions binding path.
    public string ServiceType => "Catalog";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

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
