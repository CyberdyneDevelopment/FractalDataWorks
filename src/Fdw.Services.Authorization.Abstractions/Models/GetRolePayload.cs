namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// The body of a read of one role: <c>POST /roles/get</c>.
/// </summary>
/// <remarks>
/// A read of one record is a POST, not a GET: a GET is a plain paged list and nothing else.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class GetRolePayload
{
    /// <summary>Gets or sets the role's name or its id.</summary>
    public string Name { get; set; } = string.Empty;
}
