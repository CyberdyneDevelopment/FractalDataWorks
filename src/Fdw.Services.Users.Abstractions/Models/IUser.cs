using System;

namespace Fdw.Services.Users.Models;

/// <summary>
/// User interface.
/// </summary>
public interface IUser
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets a value indicating whether the user is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the last login timestamp.
    /// </summary>
    DateTimeOffset? LastLoginAt { get; }

    /// <summary>
    /// Gets the account creation timestamp.
    /// </summary>
    DateTimeOffset CreatedAt { get; }
}
