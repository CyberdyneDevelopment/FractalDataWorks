namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Pagination information extracted from REST API responses.
/// </summary>
/// <param name="TotalCount">The total number of items, if available.</param>
/// <param name="NextCursor">The cursor or URL for the next page, if available.</param>
public record RestPaginationInfo(int? TotalCount, string? NextCursor);