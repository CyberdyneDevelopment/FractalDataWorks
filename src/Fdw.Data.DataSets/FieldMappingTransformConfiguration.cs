using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets;

/// <summary>
/// Configuration for a single transform step in a field mapping's transform chain.
/// </summary>
/// <remarks>
/// <para>
/// FieldMappingTransformConfiguration is a child of DataSetFieldMappingConfiguration.
/// Each field mapping can have an ordered chain of transforms that are applied sequentially
/// to the raw field value during ETL processing.
/// </para>
/// <para>
/// The <see cref="TransformType"/> must be a valid <see cref="Fdw.Data.Abstractions.TransformationTypes"/>
/// name that descends from <see cref="FieldTransformationBase"/>. Parameters for the transform
/// are stored in a child <see cref="FieldMappingTransformParameterConfiguration"/> table.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSet",
    ServiceType = "FieldMappingTransform")]
public sealed partial class FieldMappingTransformConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this transform step.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent field mapping identifier.
    /// </summary>
    /// <remarks>
    /// Foreign key to data.DataSetFieldMapping.Id.
    /// </remarks>
    public Guid DataSetFieldMappingId { get; set; }

    /// <summary>
    /// Gets or sets the transform type name.
    /// Must be a valid TransformationTypes name that descends from FieldTransformationBase.
    /// </summary>
    public string TransformType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order within the transform chain.
    /// Transforms are applied in ascending ordinal order.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current active version of the record.
    /// </summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this record has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the original creation date from the source system (if migrated).
    /// </summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>
    /// Gets the timestamp when the record was created in this system.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Gets the database user who created the record.
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets the application user on whose behalf the record was created.
    /// </summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the record was last modified.
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// Gets or sets the database user who last modified the record.
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application user on whose behalf the record was last modified.
    /// </summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
