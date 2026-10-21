using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// A retention policy as the endpoints return it (FDW-1575).
/// </summary>
public class RetentionPolicyResponse
{
    /// <summary>Gets or sets the policy's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the document type the policy covers.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the whole years of the period.</summary>
    public int Years { get; set; }

    /// <summary>Gets or sets the whole months of the period.</summary>
    public int Months { get; set; }

    /// <summary>Gets or sets the days of the period.</summary>
    public int Days { get; set; }

    /// <summary>Gets or sets the document date the period counts from.</summary>
    public string StartField { get; set; } = string.Empty;

    /// <summary>Gets or sets the action the application performs when the period ends.</summary>
    public string EndAction { get; set; } = string.Empty;
}
