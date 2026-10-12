using Fdw.Configuration;
﻿using System;
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
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Data.Transformations;

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
    public static string ConfigurationConnection => DataStoreTypes.ConfigurationConnection;

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
            // Why every option: DataStore has one implementation table, so one implementation provider
            // serves every kind, and the kinds are whichever options attached -- including ones declared
            // outside FDW. DataSet is not like that any more: its three kinds are three implementations.
            builder.Services.TryAddSingleton<DataStoreImplementationConfigurationProvider>(sp =>
                new DataStoreImplementationConfigurationProvider(
                    sp.GetRequiredService<ILogger<DataStoreImplementationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "DataStoreConfiguration"));
            builder.Services.TryAddSingleton<IImplementationConfigurationProvider<IDataStoreImplementationConfiguration>>(
                sp => sp.GetRequiredService<DataStoreImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<DataStoreConfigurationProvider>(sp =>
            {
                var domain = new DataStoreConfigurationProvider(
                    sp.GetRequiredService<ILogger<DataStoreConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "DataStore");
                var implementation = sp.GetRequiredService<IImplementationConfigurationProvider<IDataStoreImplementationConfiguration>>();
                foreach (var kind in DataStoreTypes.All())
                    domain.Register(kind.Name, implementation);
                return domain;
            });
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>>(
                sp => sp.GetRequiredService<DataStoreConfigurationProvider>());

            // Each kind of data set is its own implementation with its own table and provider (FDW-1031); the
            // domain row says which kind a data set is, and the domain provider reads that kind's table.
            builder.Services.TryAddSingleton<SimpleDataSetConfigurationProvider>(sp =>
                new SimpleDataSetConfigurationProvider(
                    sp.GetRequiredService<ILogger<SimpleDataSetConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "SimpleDataSetConfiguration"));
            builder.Services.TryAddSingleton<CompoundDataSetConfigurationProvider>(sp =>
                new CompoundDataSetConfigurationProvider(
                    sp.GetRequiredService<ILogger<CompoundDataSetConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "CompoundDataSetConfiguration"));
            builder.Services.TryAddSingleton<FederatedDataSetConfigurationProvider>(sp =>
                new FederatedDataSetConfigurationProvider(
                    sp.GetRequiredService<ILogger<FederatedDataSetConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "FederatedDataSetConfiguration"));
            builder.Services.TryAddSingleton<DataSetConfigurationProvider>(sp =>
            {
                var domain = new DataSetConfigurationProvider(
                    sp.GetRequiredService<ILogger<DataSetConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "DataSet");
                domain.Register(SimpleDataSetConfiguration.ImplementationName,
                    sp.GetRequiredService<SimpleDataSetConfigurationProvider>());
                domain.Register(CompoundDataSetConfiguration.ImplementationName,
                    sp.GetRequiredService<CompoundDataSetConfigurationProvider>());
                domain.Register(FederatedDataSetConfiguration.ImplementationName,
                    sp.GetRequiredService<FederatedDataSetConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<IDataSetImplementationConfiguration>>(
                sp => sp.GetRequiredService<IDataSetConfigurationProvider>());

            // A variation is a view of a data set, in its own domain so it never appears among the data sets
            // (FDW-1022). Each kind is an implementation with its own table (FDW-1026): Scoped, a data set a
            // workspace owns as its own view of a main one, and DataView, a saved chart owned by a user. The domain
            // row says which kind a variation is.
            builder.Services.TryAddSingleton<ScopedDataSetVariationConfigurationProvider>(sp =>
                new ScopedDataSetVariationConfigurationProvider(
                    sp.GetRequiredService<ILogger<ScopedDataSetVariationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "ScopedDataSetVariationConfiguration"));
            builder.Services.TryAddSingleton<DataViewDataSetVariationConfigurationProvider>(sp =>
                new DataViewDataSetVariationConfigurationProvider(
                    sp.GetRequiredService<ILogger<DataViewDataSetVariationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "DataViewDataSetVariationConfiguration"));
            builder.Services.TryAddSingleton<DataSetVariationConfigurationProvider>(sp =>
            {
                var domain = new DataSetVariationConfigurationProvider(
                    sp.GetRequiredService<ILogger<DataSetVariationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "data", "DataSetVariation");
                domain.Register(ScopedDataSetVariationConfiguration.ImplementationName,
                    sp.GetRequiredService<ScopedDataSetVariationConfigurationProvider>());
                domain.Register(DataViewDataSetVariationConfiguration.ImplementationName,
                    sp.GetRequiredService<DataViewDataSetVariationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IDataSetVariationConfigurationProvider>(
                sp => sp.GetRequiredService<DataSetVariationConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<IDataSetVariationImplementationConfiguration>>(
                sp => sp.GetRequiredService<IDataSetVariationConfigurationProvider>());

            // A transformation is defined once and used later (FDW-846, FDW-962). Field transforms one
            // column and is attached to field mappings; Map, Filter, Calculate, Aggregate and Lookup
            // transform whole inputs and run in pipeline stages. Each is an implementation of the one
            // Transformation domain, and each implementation's child rows hang off its own row.
            builder.Services.TryAddSingleton<FieldTransformationConfigurationProvider>(sp =>
                new FieldTransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<FieldTransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "FieldTransformationConfiguration"));
            builder.Services.TryAddSingleton<MapTransformationConfigurationProvider>(sp =>
                new MapTransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<MapTransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "MapTransformationConfiguration"));
            builder.Services.TryAddSingleton<FilterTransformationConfigurationProvider>(sp =>
                new FilterTransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<FilterTransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "FilterTransformationConfiguration"));
            builder.Services.TryAddSingleton<CalculateTransformationConfigurationProvider>(sp =>
                new CalculateTransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<CalculateTransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "CalculateTransformationConfiguration"));
            builder.Services.TryAddSingleton<BuiltTransformationConfigurationProvider>(sp =>
                new BuiltTransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<BuiltTransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "BuiltTransformationConfiguration"));
            builder.Services.TryAddSingleton<AggregateTransformationConfigurationProvider>(sp =>
                new AggregateTransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<AggregateTransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "AggregateTransformationConfiguration"));
            builder.Services.TryAddSingleton<LookupTransformationConfigurationProvider>(sp =>
                new LookupTransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<LookupTransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "LookupTransformationConfiguration"));
            builder.Services.TryAddSingleton<TransformationConfigurationProvider>(sp =>
            {
                var domain = new TransformationConfigurationProvider(
                    sp.GetRequiredService<ILogger<TransformationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "transform", "Transformation");
                domain.Register(FieldTransformationConfiguration.ImplementationName,
                    sp.GetRequiredService<FieldTransformationConfigurationProvider>());
                domain.Register(MapTransformationConfiguration.ImplementationName,
                    sp.GetRequiredService<MapTransformationConfigurationProvider>());
                domain.Register(FilterTransformationConfiguration.ImplementationName,
                    sp.GetRequiredService<FilterTransformationConfigurationProvider>());
                domain.Register(CalculateTransformationConfiguration.ImplementationName,
                    sp.GetRequiredService<CalculateTransformationConfigurationProvider>());
                domain.Register(BuiltTransformationConfiguration.ImplementationName,
                    sp.GetRequiredService<BuiltTransformationConfigurationProvider>());
                domain.Register(AggregateTransformationConfiguration.ImplementationName,
                    sp.GetRequiredService<AggregateTransformationConfigurationProvider>());
                domain.Register(LookupTransformationConfiguration.ImplementationName,
                    sp.GetRequiredService<LookupTransformationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ITransformationConfigurationProvider>(
                sp => sp.GetRequiredService<TransformationConfigurationProvider>());

            // What makes an attached transformation apply on a data-set read (FDW-877).
            builder.Services.TryAddSingleton<FieldMappingTransformationResolver>();

            // Lineage reads containers that span domains and, in the transform schema, have no
            // configuration types of their own. It is registered beside the DataSet provider because
            // that is the closest thing to an owner it has.
            builder.Services.TryAddSingleton<LineageConfigurationProvider>(sp =>
                new LineageConfigurationProvider(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    sp.GetRequiredService<IDataSetConfigurationProvider>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
