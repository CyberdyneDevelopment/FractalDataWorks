using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>
/// A threshold attached to a subject (FDW-3936): one row of the link table of the subject's kind, holding its own Id and the two Ids and nothing else. The link tables of all
/// kinds have these columns, so this one class reads them all; the table is named by the subject kind.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class ThresholdLinkConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the link's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the Id of the subject in its domain table.</summary>
    public Guid SubjectId { get; set; }

    /// <summary>Gets or sets the Id of the threshold.</summary>
    public Guid ThresholdId { get; set; }

    /// <summary>Gets or sets the tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets a value indicating whether the link is the current one.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the link was retired.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the source create date.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets who created it.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets on whose behalf it was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets when it was last changed.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets who changed it last.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets on whose behalf it was changed last.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
