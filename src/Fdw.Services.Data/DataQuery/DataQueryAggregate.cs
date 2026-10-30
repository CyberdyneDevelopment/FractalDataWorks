namespace Fdw.Services.Data.Endpoints;

/// <summary>One aggregate of a grouped data query: a function over a field, under a name the result carries it by.</summary>
public sealed class DataQueryAggregate
{
    /// <summary>Gets or sets the function: <c>Sum</c>, <c>Avg</c>, <c>Min</c>, <c>Max</c> or <c>Count</c>. Required.</summary>
    public string Function { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data set field the function reads. Required for every function but <c>Count</c>,
    /// where none counts every row of the group.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the name the aggregate has in each result row. Required: letters, digits and underscores,
    /// not starting with a digit, and different from every group field and every other aggregate's name.
    /// </summary>
    public string As { get; set; } = string.Empty;
}
