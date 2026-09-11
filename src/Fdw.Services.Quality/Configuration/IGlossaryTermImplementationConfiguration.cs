using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Quality.Configuration;

/// <summary>The contract every GlossaryTerm implementation carries.</summary>
public interface IGlossaryTermImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid GlossaryTermId { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Definition.</summary>
    string Definition { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Formula.</summary>
    string? Formula { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Category.</summary>
    string Category { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Owner.</summary>
    string Owner { get; set; }

    /// <summary>Gets or sets the GlossaryTerm Steward.</summary>
    string Steward { get; set; }

    /// <summary>Gets or sets the GlossaryTerm RelatedTerms.</summary>
    IList<GlossaryTermRelationConfiguration> RelatedTerms { get; set; }

    /// <summary>Gets or sets the GlossaryTerm LinkedDataSets.</summary>
    IList<GlossaryTermLinkedDataSetConfiguration> LinkedDataSets { get; set; }

    /// <summary>The domain record's row id -- the foreign key the constraint is on.</summary>
    int GlossaryTermRowId { get; set; }
}
