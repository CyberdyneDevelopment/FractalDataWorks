using System.Collections.Generic;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>The rule types and severities a caller may use, as this host has them registered.</summary>
/// <remarks>
/// Exists so a client does not hardcode the set. A picker built from a copy of the list goes stale
/// the moment a tenth type is registered, and a rule stored with a type the server does not
/// recognise sits in the list looking correct and never runs — which is exactly what a hardcoded
/// picker eventually produces.
///
/// Read from the TypeCollections rather than a literal, so this answers with what the host can
/// actually accept rather than with what someone wrote down.
/// </remarks>
public class QualityRuleTypesResponse
{
    /// <summary>Gets or sets the registered rule types.</summary>
    public IList<QualityRuleTypeDto> RuleTypes { get; set; } = [];

    /// <summary>Gets or sets the registered severities.</summary>
    public IList<QualitySeverityDto> Severities { get; set; } = [];
}
