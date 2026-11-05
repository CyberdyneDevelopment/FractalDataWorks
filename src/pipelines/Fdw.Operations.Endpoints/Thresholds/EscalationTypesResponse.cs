using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Every consequence a rule can fire (FDW-3948).</summary>
public sealed class EscalationTypesResponse
{
    /// <summary>Gets or sets the consequences, by name.</summary>
    public IReadOnlyList<EscalationTypeResponse> Types { get; set; } = [];
}
