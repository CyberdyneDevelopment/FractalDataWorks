using FastEndpoints;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request for one page of an account's sign-ins, newest first (FDW-2729).
/// </summary>
public class ListUserSignInsRequest : UserScopedRequest
{
    /// <summary>The most sign-ins one page may hold.</summary>
    public const int MaxTake = 200;

    /// <summary>The page size when the caller names none.</summary>
    public const int DefaultTake = 50;

    /// <summary>Gets or sets how many of the newest sign-ins precede the page.</summary>
    [QueryParam]
    public int Skip { get; set; }

    /// <summary>Gets or sets how many sign-ins the page holds at most.</summary>
    [QueryParam]
    public int Take { get; set; } = DefaultTake;
}
