using System;
using System.ComponentModel.DataAnnotations.Schema;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Pure relationship row: which key, which field, ordinal in composite key.
/// Maps to <c>data.DataContainerKeyField</c>. Key identity (Name, TypeId, ReferencedKeyId)
/// lives on the parent <see cref="DataContainerKeyConfiguration"/>.
/// </summary>
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataContainerKeyField")]
public partial class DataContainerKeyFieldConfiguration : IGenericConfiguration
{

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public Guid DataContainerKeyId { get; set; }


    /// <inheritdoc />
    public Guid DataContainerFieldId { get; set; }


    /// <inheritdoc />
    public int Ordinal { get; set; }

    /// <summary>Whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// JSON-bound: name of the participating field on the owning container. Loader resolves this
    /// to <see cref="DataContainerFieldId"/> at startup by looking up the field by name within
    /// the same container.
    /// </summary>
    [NotMapped]
    public string Name { get; set; } = string.Empty;
    /// <summary>Section name for binding.</summary>
    public string SectionName { get; set; } = "DataContainerKeyFields";

    /// <summary>Service type.</summary>
    public string ServiceType { get; set; } = "DataContainerKeyField";

    /// <summary>Optional service option type.</summary>
    public string? ServiceOptionType { get; set; }

}
