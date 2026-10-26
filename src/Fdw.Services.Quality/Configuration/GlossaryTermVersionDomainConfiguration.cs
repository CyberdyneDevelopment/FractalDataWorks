using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// The domain row of a version of a glossary term (FDW-2331): the base term it varies, the place it is held at, and the fields a list of versions
/// shows. Stored in catalog.GlossaryTermVersion.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class GlossaryTermVersionDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the logical Id of the glossary term this is a version of.</summary>
    public Guid BaseTermId { get; set; }

    /// <summary>Gets or sets the plain name of that term; the stored name joins it to the place.</summary>
    public string TermName { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of place the version is held at: Tenant, Organization or Workspace.</summary>
    public string ScopeKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the tenant, organization or workspace the version is held at.</summary>
    public Guid ScopeId { get; set; }

    /// <summary>Gets or sets the business definition of the term at that place.</summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>Gets or sets the business owner of the term at that place.</summary>
    public string Owner { get; set; } = string.Empty;
}
