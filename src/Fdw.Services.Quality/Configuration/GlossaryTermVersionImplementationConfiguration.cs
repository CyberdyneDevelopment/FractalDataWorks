using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// A version of a glossary term held at a tenant, organization or workspace (FDW-2331). Stored in catalog.GlossaryTermVersion and
/// catalog.GlossaryTermVersionConfiguration.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Catalog", ServiceType = "GlossaryTermVersion")]
public sealed partial class GlossaryTermVersionImplementationConfiguration : IGlossaryTermVersionImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of this version.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the stored name: the term's name joined to the place, unique per place.</summary>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid BaseTermId { get; set; }

    /// <inheritdoc />
    public string TermName { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ScopeKind { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid ScopeId { get; set; }

    /// <inheritdoc />
    public string Definition { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Formula { get; set; }

    /// <inheritdoc />
    public string Owner { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Steward { get; set; }
}
