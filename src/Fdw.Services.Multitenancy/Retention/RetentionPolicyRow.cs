using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// A row of the table of retention policies (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class RetentionPolicyRow
{
    /// <summary>Gets or sets the row's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the organization belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the organization the policy applies to.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the document type the policy covers.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the whole years of the period.</summary>
    public int PeriodYears { get; set; }

    /// <summary>Gets or sets the whole months of the period.</summary>
    public int PeriodMonths { get; set; }

    /// <summary>Gets or sets the days of the period.</summary>
    public int PeriodDays { get; set; }

    /// <summary>Gets or sets the document date the period counts from.</summary>
    public string StartField { get; set; } = string.Empty;

    /// <summary>Gets or sets the action performed when the period ends.</summary>
    public string EndAction { get; set; } = string.Empty;
}
