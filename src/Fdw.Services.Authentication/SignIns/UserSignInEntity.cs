using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication.SignIns;

/// <summary>
/// One row of <c>auth.UserSignIn</c> (AuthDb): a person signed in (FDW-2729).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class UserSignInEntity
{
    /// <summary>Gets or sets the sign-in's identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the person who signed in.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets when the person signed in.</summary>
    public DateTimeOffset SignedInAt { get; set; }

    /// <summary>Gets or sets the Session authentication service the sign-in was issued into; null when the flow issued a bearer token.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Gets or sets the flow that authenticated the person.</summary>
    public string FlowName { get; set; } = string.Empty;

    /// <summary>Gets or sets the address the request came from; null when it was not known.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the User-Agent header as the caller sent it; null when none was sent.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Gets or sets the browser the User-Agent names; null when it names none of the known browsers.</summary>
    public string? Browser { get; set; }

    /// <summary>Gets or sets the operating system the User-Agent names; null when it names none of the known systems.</summary>
    public string? OperatingSystem { get; set; }
}
