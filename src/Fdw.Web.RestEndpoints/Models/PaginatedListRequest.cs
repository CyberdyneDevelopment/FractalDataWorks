namespace Fdw.Web.RestEndpoints.Models;

/// <summary>
/// Request model for paginated list endpoints.
/// Provides skip/take parameters for offset-based pagination.
/// </summary>
public sealed class PaginatedListRequest
{
    /// <summary>
    /// The default number of items to return when <see cref="Take"/> is not specified.
    /// </summary>
    public const int DefaultTake = 100;

    /// <summary>
    /// Gets or sets the number of items to skip.
    /// Defaults to 0 (start from the beginning).
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to return.
    /// Defaults to null, which resolves to <see cref="DefaultTake"/> (100).
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// Gets the effective take value, applying the default when <see cref="Take"/> is null.
    /// </summary>
    public int EffectiveTake => Take ?? DefaultTake;
}
