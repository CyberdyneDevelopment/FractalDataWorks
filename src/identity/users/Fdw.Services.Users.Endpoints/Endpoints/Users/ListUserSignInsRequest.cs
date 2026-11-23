using FastEndpoints;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request for one page of an account's sign-ins, newest first (FDW-2729).
/// </summary>
public class ListUserSignInsRequest : UserScopedRequest
{
    /// <summary>Gets or sets how many of the newest sign-ins precede the page.</summary>
    [QueryParam]
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets how many sign-ins the page holds at most: from 1 to the configured most, or left out for the configured default
    /// (<c>Users:Default:SignInPageMaxTake</c> and <c>SignInPageDefaultTake</c>, FDW-7103).
    /// </summary>
    [QueryParam]
    public int? Take { get; set; }
}
