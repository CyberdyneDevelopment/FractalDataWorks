using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Data.DataSets;

/// <summary>
/// Configuration for a single parameter of a field mapping transform.
/// </summary>
/// <remarks>
/// <para>
/// FieldMappingTransformParameterConfiguration is a child of FieldMappingTransformConfiguration.
/// Each transform step can have multiple parameters that control its behavior. Parameter names
/// must match the <c>FieldTransformationBase.ExpectedParameters</c> definitions on the
/// parent transform's TypeOption.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSet",
    ServiceType = "FieldMappingTransformParameter")]
public sealed partial class FieldMappingTransformParameterConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this parameter.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent transform step identifier.
    /// </summary>
    /// <remarks>
    /// Foreign key to transform.FieldMappingTransform.Id.
    /// </remarks>
    public Guid FieldMappingTransformId { get; set; }

    /// <summary>
    /// Gets or sets the parameter name.
    /// Must match one of the parent transform's ExpectedParameters names.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

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
