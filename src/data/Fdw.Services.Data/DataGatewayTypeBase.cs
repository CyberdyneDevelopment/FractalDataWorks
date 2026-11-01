using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Data;

/// <summary>
/// Base class for DataGateway service type definitions.
/// </summary>
/// <typeparam name="TService">The DataGateway service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating DataGateway service instances.</typeparam>
public abstract class DataGatewayTypeBase<TService, TFactory> :
    ServiceTypeBase<TService, TFactory, IServiceConfiguration>,
    IDataGatewayType
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, IServiceConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataGatewayTypeBase{TService, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of the DataGateway type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="defaultPathName">The schema this option's implementation configuration lives in.</param>
    /// <param name="defaultContainerName">This option's implementation configuration table: {Implementation}{Domain}Configuration. Required, so every option states it; its registration reads it back as Container.</param>
    /// <param name="category">The optional category.</param>
    protected DataGatewayTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string defaultPathName,
        string defaultContainerName,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "DataGateway",
               defaultPathName: defaultPathName,
               defaultContainerName: defaultContainerName)
    {
    }
}
