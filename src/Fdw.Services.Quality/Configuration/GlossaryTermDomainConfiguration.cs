using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// The glossary terms domain's record: the four properties every domain has, and the fields a list of terms filters on and shows.
/// </summary>
/// <remarks>
/// Maps to <c>catalog.GlossaryTerm</c> in ConfigurationDb. A list of terms needs a term's definition, what it stands for, whether it is
/// approved, its owner and its category, not its formula, steward, related terms or linked data sets. Those fields live here, on the
/// domain row, so a page of terms, a search of their text and a narrowing by acronym, approval or category are one read of one table.
/// Reading a term stamps them onto its implementation; saving one whose values differ writes a new version of this row from them. The
/// implementation's table does not hold them (FDW-2336).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class GlossaryTermDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the business definition of the term.</summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity of the category the term sits under: a node of the <c>Glossary</c> category tree. Null when it sits under none.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the business owner of the term.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets what the term's name stands for; an acronym states it. Null for a term that is not an acronym.</summary>
    public string? StandsFor { get; set; }

    /// <summary>Gets or sets whether an acronym is on the approved list. Null for a term that is not an acronym.</summary>
    public bool? IsApproved { get; set; }
}
