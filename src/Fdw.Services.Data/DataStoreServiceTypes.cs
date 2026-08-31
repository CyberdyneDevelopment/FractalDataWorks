using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Services.Connections.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Data.Commands;

namespace Fdw.Services.Data;

/// <summary>
/// Collection of datastore service types — the services that serve what configuration describes.
/// </summary>
/// <remarks>
/// Datastores and datasets are described by configuration rather than chosen from a set of kinds, so
/// this collection has one option. It exists to own the registrations: the configuration providers for
/// both, and the two services that read them and hand what they read to a builder.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(DataStoreServiceTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>),
    typeof(IDataStoreServiceType),
    typeof(DataStoreServiceTypes),
    ServiceCategory = "DataStoreService")]
public partial class DataStoreServiceTypes : ServiceTypeCollectionBase<
    DataStoreServiceTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
    IDataStoreServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection
    {
        get => DataStoreTypes.ConfigurationConnection;
        set => DataStoreTypes.ConfigurationConnection = value;
    }

    /// <summary>
    /// Sets this collection's Register body: the option collect, then the configuration providers and
    /// the services over them.
    /// </summary>
    static DataStoreServiceTypes()
    {
        var collectOptions = RegisterFunc;

        Registration((builder, loggerFactory) =>
        {
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            // Why here: the collection owns what there is one of per domain. A provider that
            // registers itself is a second place to look and a second thing to keep in step.
            // Why literal: the child-type providers below are plain ImplementationConfigurationProviderBase<,>
            // instances (not domain-specific subclasses), so there is no per-domain constructor default
            // to fall back on — this is the domain's own default location.

            builder.Services.TryAddSingleton<DataStoreConfigurationProvider>(sp =>
                new DataStoreConfigurationProvider(
                    sp.GetService<ILogger<DataStoreConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    new Lazy<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>(
                        () => sp.GetRequiredService<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>()),
                    DataStoreTypes.ConfigurationConnection, "data"));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataStoreConfiguration, DataStoreConfigurationCommand>>(
                sp => sp.GetRequiredService<DataStoreConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<DataStoreConfiguration>>(
                sp => sp.GetRequiredService<DataStoreConfigurationProvider>());

            // Why: Child types (DataPath/DataContainer/DataContainerField) need their own providers so
            // SchemaInformationService and MsSqlSchemaImportPersister can Save discovered schema.
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>>()
                        ?? NullLogger<ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));

            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>()
                        ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));

            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>()
                        ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));


            // Why keys are registered with DataPath, DataContainer and DataContainerField rather than with
            // connections: a container's keys are the same kind of child of the same node, and the
            // connections collection owns transports, not the data schema. The cascade resolves a child by
            // finding the ConfigurationCommands option that claims its type, so without these a container
            // that declared a key saved as NoChildCommandForType and data.DataContainerKey stayed empty.
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>>()
                        ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));

            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>>()
                        ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));

            // Why (FDW-403 slice 2): DataPathPolicy and FileTypeHandlerOverride are child tables of
            // data.DataPath using a physical FK (DataPathRowId → DataPath.RowId). Registering their
            // providers here makes them available for cascade load in FileSystemDataStoreConfigProvider
            // without the FileSystem package taking a dependency on IConfigurationGateway directly.
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>()
                        ?? NullLogger<ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));

            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>()
                        ?? NullLogger<ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));
            // Why named here rather than defaulted on the constructor: a defaulted connection is one a
            // caller inherits without saying so, and this provider is registered directly rather than by
            // a collection that would otherwise name it.

            builder.Services.TryAddSingleton<DataSetConfigurationProvider>(sp =>
                new DataSetConfigurationProvider(
                    sp.GetService<ILogger<DataSetConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data"));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<DataSetConfiguration, DataSetConfigurationCommand>>(
                sp => sp.GetRequiredService<DataSetConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<DataSetConfiguration>>(
                sp => sp.GetRequiredService<DataSetConfigurationProvider>());

            // Lineage reads containers that span domains and, in the transform schema, have no
            // configuration types of their own. It is registered beside the DataSet provider because
            // that is the closest thing to an owner it has.
            builder.Services.TryAddSingleton<LineageConfigurationProvider>(sp =>
                new LineageConfigurationProvider(
                    sp.GetRequiredService<IConfigurationGatewayProvider>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
