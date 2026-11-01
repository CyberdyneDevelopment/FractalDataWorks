using System.Collections.Generic;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// One page of an account's sign-ins, newest first (FDW-2729).
/// </summary>
public class SignInPageResponse
{
    /// <summary>Gets or sets how many of the newest sign-ins precede the page.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets how many sign-ins the page was asked to hold at most.</summary>
    public int Take { get; set; }

    /// <summary>Gets or sets a value indicating whether older sign-ins follow this page.</summary>
    public bool HasMore { get; set; }

    /// <summary>Gets or sets the sign-ins on the page, newest first.</summary>
    public List<SignInResponse> Items { get; set; } = [];
}
