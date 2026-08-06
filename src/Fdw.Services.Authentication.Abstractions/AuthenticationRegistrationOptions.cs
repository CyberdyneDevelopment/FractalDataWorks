using System;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Concrete implementation of registration options for authentication services.
/// </summary>
// Why: pure options POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AuthenticationRegistrationOptions : RegistrationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationRegistrationOptions"/> class.
    /// </summary>
    public AuthenticationRegistrationOptions() : base(ServiceLifetime.Scoped) { }
}
