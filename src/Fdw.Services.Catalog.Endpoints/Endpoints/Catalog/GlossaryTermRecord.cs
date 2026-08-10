using System;
using Fdw.Data;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Database record representing a glossary term from catalog.GlossaryTerm.</summary>
[GenerateMapper]
public class GlossaryTermRecord
{

    /// <summary>Gets or sets the durable logical identifier of the glossary term.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the glossary term name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the definition of the glossary term.</summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional calculation formula for the term.</summary>
    public string? Formula { get; set; }

    /// <summary>Gets or sets the category the glossary term belongs to.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the business owner of the glossary term.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets the data steward responsible for the glossary term.</summary>
    public string? Steward { get; set; }

    /// <summary>Gets or sets whether this is the current version of the record.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the source system creation date.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the date and time the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the date and time the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}