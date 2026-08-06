using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// Data transfer object for creating a new user.
/// </summary>
// Why not sealed: CreateUserRequestValidator<TRequest> constrains TRequest to this type so a host
// can validate an extended request. Sealing it would close that extension point.
public class CreateUserRequest
{
    /// <summary>
    /// Gets or sets the username for the new user.
    /// </summary>
    // Why: 50 matches the server contract (CreateUserRequest [MaxLength(50)] + CreateUserRequestValidator
    // .MaximumLength(50)). The client previously advertised 100, so a 51-100 character username passed
    // client validation and was then rejected 400 by the server. The usr.Users.Username column is
    // nvarchar(200), so 50 is a policy limit, not a storage limit -- keep the two sides in step.
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the password for the new user.
    /// </summary>
    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    /// <summary>
    /// Gets or sets the email address for the new user.
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the initial roles to assign to the new user.
    /// </summary>
    // Why IList and why the "User" default: this is now the single declaration the server endpoint
    // binds too, and FastEndpoints needs a mutable collection. The default preserves the server
    // contract's existing behaviour exactly -- a create request that omits roles has always been
    // treated as ["User"]. NOTE for review: that default is an implicit fallback on a
    // security-relevant field and is a candidate for removal under the no-fallbacks rule, but
    // changing it here would silently alter who gets what role, so it is left as-is deliberately.
    public IList<string> Roles { get; set; } = new List<string> { "User" };
}
