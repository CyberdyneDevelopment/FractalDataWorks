namespace Fdw.Services.Authentication.Clients;

using System;
using Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Event arguments for authentication state changes.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AuthStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current user info, or <c>null</c> if the user has logged out.
    /// </summary>
    public UserInfo? User { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="user">The current user info.</param>
    public AuthStateChangedEventArgs(UserInfo? user)
    {
        User = user;
    }
}
