using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Limits;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Results;

using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Commands;
namespace Fdw.Services.Data;

/// <summary>
/// The data gateway implementation this framework ships. Registers <see cref="IDataGateway"/>,
/// <see cref="IDataStoreProvider"/>, and <see cref="ISchemaInformationService"/>
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(DataGatewayServiceTypes), "Main")]
public sealed class MainDataGatewayServiceTypeOption : DataGatewayTypeBase<IGenericService, IDataGatewayFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainDataGatewayServiceTypeOption"/> class.
    /// </summary>
    public MainDataGatewayServiceTypeOption()
        : base(
            "Main",
            "DataGateway:Main",
            "Main DataGateway",
            "The data gateway this framework ships, with DataStoreProvider, SchemaInformation and DataSetResolver")
    {
        // Initialize, because both providers have to be resolvable: the option is the only thing
        // that knows which implementation it is, and the domain provider dispatches by the name
        // registered here. Without it the domain record names a kind the registry never heard of.
        Initialization((host, loggerFactory) =>
        {
            host.Services.GetRequiredService<IDataGatewayConfigurationProvider>()
                .Register(Name, host.Services.GetRequiredService<MainDataGatewayConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<MainDataGatewayConfigurationProvider>(sp =>
                new MainDataGatewayConfigurationProvider(
                    sp.GetService<ILogger<MainDataGatewayConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataGatewayServiceTypes.ConfigurationConnection));


            // Why the domain provider and not the implementation one: the domain record says which
            // implementation this host runs, and routing to it is the domain provider's job. Reading
            // the implementation directly would name one in code and make the record decorative.
            builder.Services.TryAddSingleton(sp =>
            {
#pragma warning disable VSTHRD002
                var result = sp.GetRequiredService<IDataGatewayConfigurationProvider>()
                    .Get("DataGateway").GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                if (result.IsFailure || result.Value is null)
                {
                    throw new InvalidOperationException(
                        "DataGateway is not configured on the server tier. Whether the gateway caches is configuration, not a default.");
                }

                return result.Value;
            });


            builder.Services.TryAddScoped<ISchemaInformationService, SchemaInformationService>();

            builder.Services.AddMemoryCache();

            builder.Services.TryAddSingleton<DataGatewayResultCache>();
            builder.Services.AddSingleton<ICacheInvalidator>(sp => sp.GetRequiredService<DataGatewayResultCache>());

            builder.Services.TryAddSingleton<IConnectionLimitResolver, PassThroughConnectionLimitResolver>();
            builder.Services.TryAddSingleton<ConnectionLimitCounterStore>();

            // Why a factory and not a scoped IDataGateway registration: Create() builds a brand new
            // gateway on every call, so nothing here is ever captured across a scope boundary. The
            // factory is singleton -- it holds only the other singleton-safe pieces a gateway is
            // built from -- and MainDataGatewayProvider, also singleton, simply calls it on every ask.
            builder.Services.TryAddSingleton<IDataGatewayFactory, DataGatewayFactory>();

            builder.Services.TryAddSingleton<IDataGatewayProvider, MainDataGatewayProvider>();

            builder.Services.AddHostedService(sp =>
                new DailyLimitResetJob(
                    sp.GetRequiredService<ConnectionLimitCounterStore>(),
                    sp.GetService<ILoggerFactory>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }

}
