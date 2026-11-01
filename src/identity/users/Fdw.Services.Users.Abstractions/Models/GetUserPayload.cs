namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// The body of a read of one user: <c>POST /users/get</c>.
/// </summary>
/// <remarks>
/// A read of one record is a POST, not a GET: a GET is a plain paged list and nothing else.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class GetUserPayload
{
    /// <summary>Gets or sets the user's id or username.</summary>
    public string IdOrName { get; set; } = string.Empty;
}
