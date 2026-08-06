namespace Fdw.Operations.Endpoints;

/// <summary>
/// Request to expand a single lineage node, returning its direct upstream and downstream neighbors.
/// Bound from query string parameters: <c>?nodeType=Pipeline&amp;nodeId=MY_PIPELINE</c>.
/// </summary>
public class ExpandLineageNodeRequest
{
    /// <summary>Gets or sets the node type (e.g., Pipeline, DataSet, Connection, Calculation).</summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name-based identifier of the node to expand.</summary>
    public string NodeId { get; set; } = string.Empty;
}
