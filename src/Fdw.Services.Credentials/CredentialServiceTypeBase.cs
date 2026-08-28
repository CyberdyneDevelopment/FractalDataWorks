using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Credentials.Abstractions;

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
}
