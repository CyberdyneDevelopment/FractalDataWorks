namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Client-side mirror of the downstream consumer payload returned by field lineage.
/// </summary>
public sealed class FieldConsumerPayload
{
    /// <summary>The kind of consumer: "DataSet", "Pipeline", or "Calculation".</summary>
    public string ConsumerKind { get; set; } = string.Empty;

    /// <summary>Name of the consuming entity.</summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>Name of the specific consumer field/column, when applicable.</summary>
    public string ConsumerField { get; set; } = string.Empty;
}
