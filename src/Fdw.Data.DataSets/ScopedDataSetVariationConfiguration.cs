using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The Scoped implementation of a data set variation: a data set a workspace owns as its own view of a main data set.
/// </summary>
/// <remarks>
/// Maps to <c>data.ScopedDataSetVariationConfiguration</c>, keyed to its domain row in <c>data.DataSetVariation</c>.
/// Every property a list shows is stamped from the domain row on a read and copied back to it on a save; the
/// implementation's own table has no such column.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSetVariation", ServiceType = "Scoped")]
public sealed partial class ScopedDataSetVariationConfiguration : IDataSetVariationImplementationConfiguration
{
    /// <summary>The implementation name a Scoped variation's domain row carries.</summary>
    public const string ImplementationName = "Scoped";

    /// <inheritdoc/>
    /// <remarks>Stamped by the provider from the domain row.</remarks>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string VariationName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? DisplayName { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public Guid? OwnerWorkspaceId { get; set; }

    /// <inheritdoc/>
    public Guid? OwnerUserId { get; set; }

    /// <inheritdoc/>
    public Guid BaseDataSetId { get; set; }

    /// <inheritdoc/>
    public Guid? ParentVariationId { get; set; }

    /// <inheritdoc/>
    /// <remarks>Stamped by the provider from the domain row.</remarks>
    public string? Filters { get; set; }

    /// <inheritdoc/>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the row.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the row was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the row.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
