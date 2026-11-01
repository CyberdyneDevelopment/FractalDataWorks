using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication.SignIns;

/// <summary>
/// One row of the view <c>auth.UserLastSignIn</c> (AuthDb): when a person last signed in (FDW-2729).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class UserLastSignInEntity
{
    /// <summary>Gets or sets the person.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets when the person last signed in.</summary>
    public DateTimeOffset LastSignInAt { get; set; }
}
