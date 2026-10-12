namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a join between two sources in a DataSet.
/// </summary>
public sealed class DataSetJoinPayload
{
    /// <summary>Gets or sets the left source name.</summary>
    public string LeftSource { get; set; } = string.Empty;
    /// <summary>Gets or sets the left field name.</summary>
    public string LeftField { get; set; } = string.Empty;
    /// <summary>Gets or sets the right source name.</summary>
    public string RightSource { get; set; } = string.Empty;
    /// <summary>Gets or sets the right field name.</summary>
    public string RightField { get; set; } = string.Empty;
    /// <summary>Gets or sets the join type.</summary>
    public string JoinType { get; set; } = "Inner";
}
