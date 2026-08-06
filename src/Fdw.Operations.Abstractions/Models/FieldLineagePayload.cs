using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Field-level lineage information.
/// </summary>
public sealed class FieldLineagePayload
{
    /// <summary>Gets or sets the logical field name.</summary>
    public string LogicalField { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical source mappings (upstream).</summary>
    public IReadOnlyList<FieldSourceMappingPayload> Sources { get; set; } = Array.Empty<FieldSourceMappingPayload>();
    /// <summary>Gets or sets the downstream consumers ("where is this field used").</summary>
    public IReadOnlyList<FieldConsumerPayload> Consumers { get; set; } = Array.Empty<FieldConsumerPayload>();
}
