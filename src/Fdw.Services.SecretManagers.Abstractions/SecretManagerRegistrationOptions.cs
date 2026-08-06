using System;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Concrete implementation of registration options for secret manager services.
/// </summary>
public sealed class SecretManagerRegistrationOptions : RegistrationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerRegistrationOptions"/> class.
    /// </summary>
    public SecretManagerRegistrationOptions() : base(ServiceLifetime.Singleton) { }
}
