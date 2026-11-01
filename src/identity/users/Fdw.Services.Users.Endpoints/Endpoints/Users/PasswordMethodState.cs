using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// What the account's password method looks like to an administrator.
/// </summary>
public class PasswordMethodState
{
    /// <summary>Gets or sets whether a password is set.</summary>
    public bool IsSet { get; set; }

    /// <summary>Gets or sets whether the next sign-in must change it.</summary>
    public bool MustChange { get; set; }

    /// <summary>Gets or sets when it was last changed, when that is known.</summary>
    public DateTimeOffset? ChangedAt { get; set; }

    /// <summary>Gets or sets whether the calling token may set a password on this account.</summary>
    public bool CanSet { get; set; }

    /// <summary>
    /// Gets or sets why <see cref="CanSet"/> is false, as a sentence to show the person.
    /// </summary>
    public string? CanSetReason { get; set; }

    /// <summary>
    /// Gets or sets whether any enabled password sign-in flow admits the account (FDW-912).
    /// </summary>
    /// <remarks>
    /// Asked of the same rule the password routes enforce (<c>ISignInMethodAdmission</c>), so a console
    /// can say why a set password does not work and stop offering one the route would refuse. Null
    /// when it could not be decided, which is not a yes.
    /// </remarks>
    public bool? IsUsable { get; set; }

    /// <summary>Gets or sets whether the calling token may remove the account's password.</summary>
    public bool CanRemove { get; set; }
}
