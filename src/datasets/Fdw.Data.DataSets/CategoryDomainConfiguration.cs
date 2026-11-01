using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The Category domain's record (FDW-1465): one node of a category tree, with everything a list of the tree shows, so the whole tree
/// reads from the domain table in one query. Its implementation is its type: DataSet or Custom.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class CategoryDomainConfiguration : DomainConfiguration
{
    /// <summary>The domain every category belongs to.</summary>
    public const string DomainName = "Category";

    /// <summary>Gets or sets what a person reads; it may repeat under different parents.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the category is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the identity of the category it sits under; null for a top-level category.</summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>Gets or sets when the category was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the tenant the category belongs to; the tree keeps one hierarchy per tenant and organisation.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the organisation (visibility group) the category belongs to; null when it belongs to the whole tenant.</summary>
    public Guid? VisibilityGroupId { get; set; }
}
