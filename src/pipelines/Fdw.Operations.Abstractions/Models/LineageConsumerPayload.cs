namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Downstream consumer in lineage.
/// </summary>
public sealed class LineageConsumerPayload
{
    /// <summary>Gets or sets the consumer name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the consumer type.</summary>
    public string ConsumerType { get; set; } = string.Empty;
}
