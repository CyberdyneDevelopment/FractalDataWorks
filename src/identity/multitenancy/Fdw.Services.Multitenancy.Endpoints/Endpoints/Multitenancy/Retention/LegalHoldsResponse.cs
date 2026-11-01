using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The legal holds of an organization (FDW-1575).
/// </summary>
public class LegalHoldsResponse
{
    /// <summary>Gets or sets the holds, newest first.</summary>
    public IReadOnlyList<LegalHoldResponse> Holds { get; set; } = [];
}
