using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Quality.Configuration;

/// <summary>The contract every GlossaryTerm implementation carries.</summary>
public interface IGlossaryTermImplementationConfiguration : IImplementationConfiguration
{

    /// <summary>Gets or sets the GlossaryTerm Definition.</summary>
    string Definition { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Formula.</summary>
    string? Formula { get; set; }

    /// <summary>
    /// Gets or sets the identity of the category the term sits under (FDW-1466): a node of the <c>Glossary</c> category tree. Null when it
    /// sits under none.
    /// </summary>
    Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Owner.</summary>
    string Owner { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Steward.</summary>
    string Steward { get; set; }

    /// <summary>
    /// Gets or sets what the term's name stands for (FDW-1682): an acronym is a glossary term that states it. Null for a term that is
    /// not an acronym.
    /// </summary>
    string? StandsFor { get; set; }

    /// <summary>
    /// Gets or sets whether an acronym is on the approved list (FDW-1682). Null for a term that is not an acronym; an acronym states it,
    /// so a flow that proposes acronyms before they are approved needs no new member.
    /// </summary>
    bool? IsApproved { get; set; }

    /// <summary>Gets or sets the GlossaryTerm RelatedTerms.</summary>
    IList<GlossaryTermRelationConfiguration> RelatedTerms { get; set; }

    /// <summary>Gets or sets the GlossaryTerm LinkedDataSets.</summary>
    IList<GlossaryTermLinkedDataSetConfiguration> LinkedDataSets { get; set; }

}
