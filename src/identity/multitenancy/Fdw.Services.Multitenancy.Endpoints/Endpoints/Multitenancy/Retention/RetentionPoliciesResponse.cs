using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The retention policies of an organization (FDW-1575).
/// </summary>
public class RetentionPoliciesResponse
{
    /// <summary>Gets or sets the policies, one per document type.</summary>
    public IReadOnlyList<RetentionPolicyResponse> Policies { get; set; } = [];
}
