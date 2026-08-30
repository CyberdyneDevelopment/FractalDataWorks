using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Data;

/// <summary>
/// Base for configuration gateway service types.
/// </summary>
/// <typeparam name="TService">The service this option produces.</typeparam>
/// <typeparam name="TFactory">The factory that produces it.</typeparam>
public abstract class ConfigurationGatewayTypeBase<TService, TFactory> :
    ServiceTypeBase<TService, TFactory, IServiceConfiguration>,
    IConfigurationGatewayType
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, IServiceConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationGatewayTypeBase{TService, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="sectionName">The configuration section this option binds from.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The category; defaults to ConfigurationGateway.</param>
    protected ConfigurationGatewayTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "ConfigurationGateway",
               defaultDataStoreName: "PlatformConfiguration")
    {
    }
}
