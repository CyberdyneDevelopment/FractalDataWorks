using System;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A node of a category tree (FDW-1465), of whichever type.</summary>
public interface ICategoryImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets what a person reads; it may repeat under different parents.</summary>
    string DisplayName { get; set; }

    /// <summary>Gets or sets what the category is for.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the identity of the category it sits under; null for a top-level category.</summary>
    Guid? ParentCategoryId { get; set; }

    /// <summary>Gets or sets when the category was created.</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the tenant the category belongs to. The domain row carries it as a list field, so every category type states it.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the organisation (visibility group) the category belongs to; null when it belongs to the whole tenant.</summary>
    Guid? VisibilityGroupId { get; set; }
}
