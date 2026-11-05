using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>A threshold (FDW-3948): what it is called, its type, whether it is judged, and its rules in the order they are judged. It is the body of a create and a replace and the answer of a read.</summary>
public sealed class ThresholdPayload
{
    /// <summary>Gets or sets the name, unique in a tenant. A route supplies it for a read, a replace and a delete.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the threshold is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the threshold type, the name of an option of ThresholdTypes. It says which subject the threshold judges and which values a rule may name.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the threshold is judged. A request states it: none is assumed.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether a rule fires only when its criteria start to hold, and again only after they stopped holding; false fires on every report that matches. A request states it: none is assumed.</summary>
    public bool? FiresOnEntry { get; set; }

    /// <summary>Gets or sets the parameters of the threshold: how its type's measure is taken. A type that takes none leaves this empty.</summary>
    public IReadOnlyList<ThresholdParameterPayload> Parameters { get; set; } = [];

    /// <summary>Gets or sets the rules, judged in this order.</summary>
    public IReadOnlyList<ThresholdRulePayload> Rules { get; set; } = [];
}
