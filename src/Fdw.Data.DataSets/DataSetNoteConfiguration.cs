using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A freeform note attached to a DataSet — operational observations, caveats,
/// or "while it is working" remarks visible to anyone browsing the DataSet.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSet",
    ServiceType = "DataSetNote")]
public sealed partial class DataSetNoteConfiguration
{
    /// <summary>Gets or sets the unique identifier for this note.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent DataSet identifier (FK to data.DataSet.Id).</summary>
    public Guid DataSetId { get; set; }

    /// <summary>Gets or sets the short title or subject line of the note.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the full note body.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
