using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// The columns changed when the policy of a document type is set again (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RetentionPolicyUpdate
{
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
