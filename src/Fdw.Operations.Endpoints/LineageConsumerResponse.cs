using System;
using System.Reflection;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Downstream consumer in lineage.
/// </summary>
public class LineageConsumerResponse
{
    /// <summary>Gets or sets the consumer name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the consumer type (e.g., Pipeline).</summary>
    public string ConsumerType { get; set; } = string.Empty;
}