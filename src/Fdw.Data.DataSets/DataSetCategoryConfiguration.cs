using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A category the platform defines for organising data sets (FDW-1465), as the domain provider composes it. Its fields live on the domain row; the implementation row holds only its
/// key.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Category", ServiceType = "DataSet")]
public sealed partial class DataSetCategoryConfiguration : ICategoryImplementationConfiguration
{
    /// <summary>The implementation this is: the category's type.</summary>
    public const string ImplementationName = "DataSet";

    /// <summary>Gets or sets the category's durable identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the category's stable name, unique per tenant.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain, which is Category.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation, which is DataSet.</summary>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public string DisplayName { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public Guid? ParentCategoryId { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the tenant the category belongs to.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the visibility group that can see the category.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the category is retired.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets when the source system created the row.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets who created the row.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets on whose behalf the row was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets when the row was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets who last modified the row.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets on whose behalf the row was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
