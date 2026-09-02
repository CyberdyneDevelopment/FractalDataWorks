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
/// Default DataGateway service type that registers <see cref="IDataGateway"/>,
/// <see cref="IDataStoreProvider"/>, and <see cref="ISchemaInformationService"/>
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(DataGatewayTypes), "Default")]
public sealed class DefaultDataGatewayServiceType : DataGatewayTypeBase<IGenericService, IDataGatewayFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataGatewayServiceType"/> class.
    /// </summary>
    public DefaultDataGatewayServiceType()
        : base(
            "Default",
            "DataGateway:Default",
            "Default DataGateway",
            "Default DataGateway with DataStoreProvider, SchemaInformation, and DataSetResolver")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<DataGatewayConfigurationProvider>(sp =>
                new DataGatewayConfigurationProvider(
                    sp.GetService<ILogger<DataGatewayConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    "ServerConfiguration"));

            // Why resolved rather than injected: the gateway takes its settings as a value and a DI
            // factory cannot await the read.
            builder.Services.TryAddSingleton(sp =>
            {
#pragma warning disable VSTHRD002
                var result = sp.GetRequiredService<DataGatewayConfigurationProvider>()
                    .Get("DataGateway").GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                if (result.IsFailure || result.Value is null)
                {
                    throw new InvalidOperationException(
                        "DataGateway is not configured on the server tier. Whether the gateway caches is configuration, not a default.");
                }

                return result.Value;
            });


            builder.Services.TryAddSingleton(sp => new Lazy<IDataSetConfigurationProvider>(() => sp.GetRequiredService<IDataSetConfigurationProvider>()));
            builder.Services.TryAddSingleton<IDataGateway, DataGatewayService>();
            builder.Services.TryAddScoped<ISchemaInformationService, SchemaInformationService>();

            builder.Services.AddMemoryCache();

            builder.Services.TryAddScoped<DataGatewayService>();

            builder.Services.TryAddSingleton<DataGatewayResultCache>();
            builder.Services.AddSingleton<ICacheInvalidator>(sp => sp.GetRequiredService<DataGatewayResultCache>());

            var existing = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IDataGateway));
            if (existing != null)
                builder.Services.Remove(existing);

            builder.Services.TryAddSingleton<IConnectionLimitResolver, PassThroughConnectionLimitResolver>();
            builder.Services.TryAddSingleton<ConnectionLimitCounterStore>();

            builder.Services.AddScoped<LimitEnforcementDataGateway>(sp =>
                new LimitEnforcementDataGateway(
                    sp.GetRequiredService<DataGatewayService>(),
                    sp.GetRequiredService<IConnectionLimitResolver>(),
                    sp.GetRequiredService<ConnectionLimitCounterStore>(),
                    sp.GetService<ILoggerFactory>()));

            builder.Services.AddScoped<IDataGateway>(sp => sp.GetRequiredService<LimitEnforcementDataGateway>());

            builder.Services.AddHostedService(sp =>
                new DailyLimitResetJob(
                    sp.GetRequiredService<ConnectionLimitCounterStore>(),
                    sp.GetService<ILoggerFactory>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }

}
