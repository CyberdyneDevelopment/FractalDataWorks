using System;
using Fdw.Configuration;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// A version of a glossary term held at a place (FDW-2331). It carries what can differ by place: definition, formula, owner and steward. The category,
/// the acronym facts, the related terms and the linked data sets belong to the base term.
/// </summary>
public interface IGlossaryTermVersionImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the logical Id of the glossary term this is a version of.</summary>
    Guid BaseTermId { get; set; }

    /// <summary>Gets or sets the plain name of that term.</summary>
    string TermName { get; set; }

    /// <summary>Gets or sets the kind of place the version is held at: Tenant, Organization or Workspace.</summary>
    string ScopeKind { get; set; }

    /// <summary>Gets or sets the id of the place the version is held at.</summary>
    Guid ScopeId { get; set; }

    /// <summary>Gets or sets the business definition at that place.</summary>
    string Definition { get; set; }

    /// <summary>Gets or sets the optional calculation formula at that place.</summary>
    string? Formula { get; set; }

    /// <summary>Gets or sets the business owner at that place.</summary>
    string Owner { get; set; }

    /// <summary>Gets or sets the data steward at that place.</summary>
    string? Steward { get; set; }
}
