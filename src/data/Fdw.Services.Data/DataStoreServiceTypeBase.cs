using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Data;

/// <summary>
/// Base for datastore service options.
/// </summary>
/// <typeparam name="TService">The service this option produces.</typeparam>
/// <typeparam name="TFactory">The factory that produces it.</typeparam>
public abstract class DataStoreServiceTypeBase<TService, TFactory> :
    ServiceTypeBase<TService, TFactory, IServiceConfiguration>,
    IDataStoreServiceType
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, IServiceConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreServiceTypeBase{TService, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="sectionName">The configuration section this option binds from.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The category; defaults to DataStoreService.</param>
    protected DataStoreServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "DataStoreService")
    {
    }
}
