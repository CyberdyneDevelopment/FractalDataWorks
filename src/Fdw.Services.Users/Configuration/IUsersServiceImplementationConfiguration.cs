using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Users.Configuration;

/// <summary>The contract every UsersService implementation carries.</summary>
public interface IUsersServiceImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid UsersServiceId { get; set; }

    /// <summary>Gets or sets the UsersService CredentialServiceName.</summary>
    string? CredentialServiceName { get; set; }

    /// <summary>Gets or sets the UsersService PasswordHashAlgorithm.</summary>
    string PasswordHashAlgorithm { get; set; }

    /// <summary>Gets or sets the UsersService PasswordMaxAgeDays.</summary>
    int PasswordMaxAgeDays { get; set; }

    /// <summary>Gets or sets the UsersService MaxFailedLoginAttempts.</summary>
    int MaxFailedLoginAttempts { get; set; }

    /// <summary>Gets or sets the UsersService LockoutDurationMinutes.</summary>
    int LockoutDurationMinutes { get; set; }
}
