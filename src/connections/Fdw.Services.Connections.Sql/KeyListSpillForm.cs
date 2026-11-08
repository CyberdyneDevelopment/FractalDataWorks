namespace Fdw.Services.Connections.Sql;

/// <summary>
/// The SQL a dialect gives a key list that goes to a table instead of to parameters (FDW-4267).
/// </summary>
/// <param name="Before">Statements that run before the statement and make the table (empty when the dialect needs none).</param>
/// <param name="Subquery">The query the statement uses in place of the parameter list: <c>col IN (&lt;Subquery&gt;)</c>.</param>
/// <param name="After">Statements that run after the statement and drop the table (empty when the dialect made none).</param>
public sealed record KeyListSpillForm(string Before, string Subquery, string After);
