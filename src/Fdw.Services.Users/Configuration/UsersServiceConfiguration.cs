using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Users.Configuration;

/// <summary>
/// How the users domain itself is configured: which credential service it resolves through, and
/// the password policy it enforces.
/// </summary>
/// <remarks>
/// These were two appsettings sections, Users and Users:PasswordPolicy, bound as IOptions. They are
/// a row on the users domain's own store now, read by the same provider that reads users -- the
/// configuration belongs to the service it configures rather than to a file the host happens to
/// ship.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "User", ServiceType = "UsersService")]
public sealed partial class UsersServiceConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    string IGenericConfiguration.SectionName => "User";

    string IGenericConfiguration.ServiceType => "User";

    string? IGenericConfiguration.ServiceOptionType => "UsersService";

    /// <summary>Gets or sets the credential service credential operations resolve through.</summary>
    public string? CredentialServiceName { get; set; }

    /// <summary>Gets or sets the algorithm passwords are hashed with.</summary>
    public string PasswordHashAlgorithm { get; set; } = string.Empty;

    /// <summary>Gets or sets how long a password stays valid, in days. Zero means it does not expire.</summary>
    public int PasswordMaxAgeDays { get; set; }

    /// <summary>Gets or sets how many failed sign-ins lock an account.</summary>
    public int MaxFailedLoginAttempts { get; set; }

    /// <summary>Gets or sets how long an account stays locked out, in minutes.</summary>
    public int LockoutDurationMinutes { get; set; }
}
