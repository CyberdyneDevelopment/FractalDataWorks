namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Which of a query's steps run in memory: those its store already applied are left out.
/// </summary>
/// <param name="Filter">Whether WHERE runs in memory.</param>
/// <param name="Order">Whether ORDER BY runs in memory.</param>
/// <param name="Page">Whether TOP and OFFSET run in memory.</param>
public readonly record struct ProjectionSteps(bool Filter, bool Order, bool Page)
{
    /// <summary>Every step in memory: the rows came from the catalog, not a store.</summary>
    public static readonly ProjectionSteps All = new(Filter: true, Order: true, Page: true);
}
