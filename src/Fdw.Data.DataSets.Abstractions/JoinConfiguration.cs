using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration for joining data from multiple sources in a federated dataset.
/// </summary>
/// <remarks>
/// <para>
/// Join configurations define how to combine data from different sources within a dataset.
/// Joins are performed in memory after fetching data from each source, using hash join
/// or nested loop algorithms depending on data size and optimizer decisions.
/// </para>
/// <para>
/// Field names use LOGICAL field names (from the dataset schema), not physical names.
/// The framework handles translation from logical to physical field names automatically.
/// </para>
/// <para>
/// Example:
/// <code>
/// {
///   "LeftSource": "SQL_Primary",
///   "LeftField": "CustomerId",
///   "RightSource": "REST_Orders",
///   "RightField": "CustomerId",
///   "JoinType": "Left"
/// }
/// </code>
/// This joins customers from SQL with orders from REST API using CustomerId.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class JoinConfiguration
{
    /// <summary>
    /// Gets or sets the name of the left source.
    /// </summary>
    /// <value>The source name from the dataset's Sources collection.</value>
    public string LeftSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name in the left source (logical field name).
    /// </summary>
    /// <value>The logical field name as defined in the dataset schema.</value>
    public string LeftField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the right source.
    /// </summary>
    /// <value>The source name from the dataset's Sources collection.</value>
    public string RightSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name in the right source (logical field name).
    /// </summary>
    /// <value>The logical field name as defined in the dataset schema.</value>
    public string RightField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the join type.
    /// </summary>
    /// <value>
    /// The type of join to perform. Supported values:
    /// <list type="bullet">
    /// <item><term>Inner</term><description>Only rows with matching keys in both sources</description></item>
    /// <item><term>Left</term><description>All rows from left source, matching rows from right source</description></item>
    /// <item><term>Right</term><description>All rows from right source, matching rows from left source</description></item>
    /// <item><term>Full</term><description>All rows from both sources</description></item>
    /// </list>
    /// </value>
    public string JoinType { get; set; } = "Inner";
}
