using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration record for a DataSet category, backed by <c>data.DataSetCategory</c>.
/// Runtime-defined categories are loaded from this table and registered into
/// <see cref="DataSetCategories"/> at startup via the three-phase provider.
/// </summary>
/// <remarks>
/// Compile-time categories are declared with
/// <c>[TypeOption(typeof(DataSetCategories), "CategoryName")]</c> and do not
/// require a database row. DB-backed categories extend the collection at runtime
/// without a recompile (Model C hybrid).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataSetCategory")]
public partial class DataSetCategoryConfiguration
{

    /// <summary>Gets or sets the stable logical identifier for this category.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the category name, used as the TypeCollection lookup key.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional human-readable description shown in admin UI.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets an optional icon identifier (e.g., a MudBlazor icon constant name).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the display sort order (ascending, lower numbers first).</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the optional tenant scoping identifier.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created in this system.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }
}
