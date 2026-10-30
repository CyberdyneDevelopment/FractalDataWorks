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
using Fdw.Services.Data.Search;
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

            // A name that is a data set or a variation of one, resolved in one place for every route that only reads (FDW-2390).
            builder.Services.TryAddSingleton<IDataSetResolver>(sp =>
                new DataSetResolver(
                    sp.GetRequiredService<IDataSetConfigurationProvider>(),
                    sp.GetRequiredService<IDataSetVariationConfigurationProvider>(),
                    sp.GetService<ILogger<DataSetResolver>>()));

            // The rows of a data set for a consumer that is not a data query, such as a calculation's input (FDW-3130): read through
            // the gateway the consumer holds, so the access is the caller's, and capped.
            builder.Services.TryAddSingleton<IDataSetRowReader>(sp =>
                new DataSetRowReader(
                    sp.GetRequiredService<IDataSetResolver>(),
                    sp.GetService<ILogger<DataSetRowReader>>(),
                    sp.GetService<IFilterCalculations>()));

            // A category is one node of a category tree (FDW-1270, FDW-1465): its own domain whose implementation is its
            // type, DataSet, Glossary or Custom. Each type's table holds only the key; the fields of a node live on the domain row, so
            // the whole tree reads in one query.
            builder.Services.TryAddSingleton<DataSetCategoryConfigurationProvider>(sp =>
                new DataSetCategoryConfigurationProvider(
                    sp.GetRequiredService<ILogger<DataSetCategoryConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "catalog", "DataSetCategoryConfiguration"));
            builder.Services.TryAddSingleton<CustomCategoryConfigurationProvider>(sp =>
                new CustomCategoryConfigurationProvider(
                    sp.GetRequiredService<ILogger<CustomCategoryConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "catalog", "CustomCategoryConfiguration"));
            builder.Services.TryAddSingleton<GlossaryCategoryConfigurationProvider>(sp =>
                new GlossaryCategoryConfigurationProvider(
                    sp.GetRequiredService<ILogger<GlossaryCategoryConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "catalog", "GlossaryCategoryConfiguration"));
            builder.Services.TryAddSingleton<CategoryConfigurationProvider>(sp =>
            {
                var domain = new CategoryConfigurationProvider(
                    sp.GetRequiredService<ILogger<CategoryConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStoreTypes.ConfigurationConnection, "catalog", "Category");
                domain.Register(DataSetCategoryConfiguration.ImplementationName,
                    sp.GetRequiredService<DataSetCategoryConfigurationProvider>());
                domain.Register(CustomCategoryConfiguration.ImplementationName,
                    sp.GetRequiredService<CustomCategoryConfigurationProvider>());
                domain.Register(GlossaryCategoryConfiguration.ImplementationName,
                    sp.GetRequiredService<GlossaryCategoryConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ICategoryConfigurationProvider>(
                sp => sp.GetRequiredService<CategoryConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<ICategoryImplementationConfiguration>>(
                sp => sp.GetRequiredService<ICategoryConfigurationProvider>());

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

            // The searches for the fields of data sets and of containers: one service of the interface per kind, taken together as the
            // sources a catalog search asks (FDW-2444).
            builder.Services.AddSingleton<IRankedSearch>(sp => new DataSetFieldSearch(
                sp.GetRequiredService<IConfigurationGatewayProvider>(), DataStoreTypes.ConfigurationConnection, "data",
                sp.GetRequiredService<ILogger<DataSetFieldSearch>>()));
            builder.Services.AddSingleton<IRankedSearch>(sp => new ContainerFieldSearch(
                sp.GetRequiredService<IConfigurationGatewayProvider>(), DataStoreTypes.ConfigurationConnection, "data",
                sp.GetRequiredService<ILogger<ContainerFieldSearch>>()));

            // The containers of all stores, one page at a time, with the names of their paths and stores (FDW-2311).
            builder.Services.AddSingleton<IDataStoreContainerPages>(sp => new DataStoreContainerPages(
                sp.GetRequiredService<IConfigurationGatewayProvider>(), DataStoreTypes.ConfigurationConnection, "data",
                sp.GetRequiredService<ILogger<DataStoreContainerPages>>()));

            // What makes an attached transformation apply on a data-set read (FDW-877).
            builder.Services.TryAddSingleton<FieldMappingTransformationResolver>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
