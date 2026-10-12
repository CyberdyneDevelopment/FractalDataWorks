using System;
using System.ComponentModel.DataAnnotations.Schema;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Pure relationship row: which key, which field, ordinal in composite key.
/// Maps to <c>data.DataContainerKeyField</c>. Name and ReferencedKeyId live on the parent
/// <see cref="DataContainerKeyConfiguration"/> only; <see cref="KeyType"/> is denormalized onto
/// this row too, because <c>UX_DataContainerKeyField_Field_Key_KeyType_Current</c> is keyed on it
/// -- a field can carry more than one key of the same field set (e.g. both a Surrogate and a
/// Natural key) and the uniqueness constraint needs the type on the row to tell those apart.
/// </summary>
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataContainerKeyField")]
public partial class DataContainerKeyFieldConfiguration : IGenericConfiguration
{

    /// <inheritdoc />
    public Guid Id { get; set; }


    /// <inheritdoc />
    public Guid DataContainerFieldId { get; set; }


    /// <inheritdoc />
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the owning key's KeyTypes option name, copied from
    /// <see cref="DataContainerKeyConfiguration.TypeId"/>. Maps to
    /// <c>data.DataContainerKeyField.KeyType</c>, a NOT NULL column the unique index above is keyed on.
    /// </summary>
    public string KeyType { get; set; } = string.Empty;

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

}
