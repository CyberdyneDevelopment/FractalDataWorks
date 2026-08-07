using Fdw.Configuration;
using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Interface for data store type definitions.
/// DataStore types define how to configure, create, and register data stores.
/// </summary>
public interface IDataStoreType : ITypeOption<Guid, IDataStoreType>
{
    /// <summary>
    /// Gets the configuration section name for appsettings.json.
    /// </summary>
    string SectionName { get; }

    /// <summary>
    /// Gets the configuration type for this data store type.
    /// </summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// Configures IOptions binding for this data store type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    void Configure(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Configures IOptions binding with logging support.
    /// </summary>
    void Configure(IServiceCollection services, IConfiguration configuration, ILoggerFactory? loggerFactory);

    /// <summary>
    /// Registers required services (factory + dependencies) with the DI container.
    /// </summary>
    IServiceCollection Register(IServiceCollection services);

    /// <summary>
    /// Supplies the per-transport <see cref="IDataStoreBuilder"/> that assembles this transport's
    /// <see cref="IDataStore"/> tree (store → paths → containers → fields → keys).
    /// </summary>
    /// <param name="logger">Logger for build diagnostics.</param>
    /// <returns>A fresh builder instance for one store build.</returns>
    /// <remarks>
    /// Why: replaces the never-called per-container <c>Build(...)</c> and the three duplicate tree
    /// builders. The MsSql option supplies an <c>MsSqlDataStoreBuilder</c>; non-SQL transports supply
    /// the generic builder. Each call returns a fresh builder (builders are single-use, stateful
    /// across <c>Configure</c>/<c>Add</c>/<c>Build</c>).
    /// </remarks>
    IDataStoreBuilder SupplyBuilder(ILogger? logger = null);

}

/// <summary>
/// Generic interface for data store types with typed configuration.
/// </summary>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
public interface IDataStoreType<TConfiguration> : IDataStoreType
    where TConfiguration : IGenericConfiguration
{
}
