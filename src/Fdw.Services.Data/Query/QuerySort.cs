namespace Fdw.Web.RestEndpoints.Query;

/// <summary>One ordering term of a query.</summary>
public sealed class QuerySort
{
    /// <summary>Gets or sets the field to order by — a field name the resource declares.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets the direction: <c>Ascending</c> or <c>Descending</c>. Required.</summary>
    public string Direction { get; set; } = string.Empty;
}
