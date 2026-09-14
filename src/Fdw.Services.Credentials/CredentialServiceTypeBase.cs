using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Credentials.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Credentials;

/// <summary>
/// Base class for credential service type definitions that inherit from ServiceTypeBase.
/// Provides credential-service-specific metadata (category, storage location).
/// </summary>
/// <typeparam name="TService">The credential service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating credential service instances.</typeparam>
/// <typeparam name="TConfiguration">The credential service configuration type.</typeparam>
/// <remarks>
/// Credential service types inherit from this class and supply type metadata in their constructors.
/// Instantiation logic belongs in factories — this class carries metadata only.
/// </remarks>
public abstract class CredentialServiceTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    ICredentialServiceType<TService, TFactory, TConfiguration>
    where TService : ICredentialService
    where TFactory : ICredentialServiceFactory<TService, TConfiguration>
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceTypeBase{TService, TFactory, TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The type option name (e.g. "Sql").</param>
    /// <param name="sectionName">The configuration section name for IOptions binding.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <param name="description">Description of what this credential service type provides.</param>
    /// <param name="category">The service category (defaults to "CredentialService").</param>
    protected CredentialServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string category = "CredentialService")
        : base(name, sectionName, displayName, description, category,
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "sec",
               defaultContainerName: "CredentialService")
    {
    }

    // ── Domain-provider/domain-configuration-provider registration ─────────────────────────────
    // An overload of Registration/Register, not a new phase and not a new method name. Stored and
    // invoked exactly like the DI-wiring Registration(Func<IHostApplicationBuilder, ...>) overload
    // already inherited from ServiceTypeBase -- this is simply a second signature of the same verb,
    // distinguished by its parameter types.

    private Func<IServiceProvider, ICredentialServiceProvider?, ICredentialServiceConfigurationProvider?, ILogger<ICredentialServiceType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, ICredentialServiceProvider?, ICredentialServiceConfigurationProvider?, ILogger<ICredentialServiceType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above reports success and leaves
    /// both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, ICredentialServiceProvider? domainProvider, ICredentialServiceConfigurationProvider? domainConfigurationProvider, ILogger<ICredentialServiceType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}
