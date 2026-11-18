using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.DataConfigurationRules;
using Fdw.Services.Data.DataOptionRows;
using Fdw.Services.Data.Abstractions.ColumnValues;
using Fdw.Services.Data.ColumnValues;
using Fdw.Services.Data.Limits;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;

using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Commands;
using Fdw.Configuration;

namespace Fdw.Services.Data;

/// <summary>
/// The data gateway implementation this framework ships. Registers <see cref="IDataGateway"/>,
/// <see cref="IDataStoreProvider"/>, and <see cref="ISchemaInformationService"/>
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(DataGatewayServiceTypes), "Main")]
public sealed class MainDataGatewayImplementation : DataGatewayTypeBase<IGenericService, IDataGatewayFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainDataGatewayImplementation"/> class.
    /// </summary>
    public MainDataGatewayImplementation()
        : base(
            DataGatewayNames.Main,
            "DataGateway:Main",
            "Main DataGateway",
            "The data gateway this framework ships, with DataStoreProvider, SchemaInformation and DataSetResolver", defaultPathName: "dg", defaultContainerName: "MainDataGatewayConfiguration")
    {
        ColumnValuesDefaultTake = 12;
        ColumnValuesMaximumTake = 200;

        // Initialize, because both providers have to be resolvable: the option is the only thing
        // that knows which implementation it is, and the domain provider dispatches by the name
        // registered here. Without it the domain record names a kind the registry never heard of.
        Initialization((host, loggerFactory) =>
        {
            host.Services.GetRequiredService<IDataGatewayConfigurationProvider>()
                .Register(Name, host.Services.GetRequiredService<IMainDataGatewayConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IMainDataGatewayConfigurationProvider, MainDataGatewayConfigurationProvider>(sp => new MainDataGatewayConfigurationProvider(sp.GetRequiredService<ILogger<MainDataGatewayConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), DataGatewayServiceTypes.ConfigurationConnection, PathName, Container));


            // Why the domain provider and not the implementation one: the domain record says which
            // implementation this host runs, and routing to it is the domain provider's job. Reading
            // the implementation directly would name one in code and make the record decorative.
            //
            // Why resolved eagerly here rather than lazily inside a DI factory: a factory delegate has
            // no GenericResult channel back to the platform, only throw -- which this codebase doesn't
            // do. Reading it now, from a snapshot of what's registered so far (same pattern as
            // SerilogLoggingType/OpenTelemetryType), lets a bad row fail loud through the ordinary
            // Register-phase GenericResult instead.
            using var built = builder.Services.BuildServiceProvider();

            var domainProvider = built.GetRequiredService<IDataGatewayConfigurationProvider>();

            // The domain compose needs "Main" registered against ITS OWN instance of the domain
            // provider before it can dispatch to it -- Initialization does the same call against the
            // real host later, for the real request-serving container. This one only lives as long
            // as `built`.
            domainProvider.Register(Name, built.GetRequiredService<IMainDataGatewayConfigurationProvider>());

            // The elevation is opened and closed around this read. Note this one is in the REGISTER
            // phase, before Build() -- a host that brackets only its post-Build Initialize calls never
            // covered this read at all. The accessor resolved from `built` is a different instance
            // than the running host's, which does not matter: AuthenticationContextAccessor's backing
            // AsyncLocal is static, so every instance reads and writes the one ambient slot.
            IGenericResult<IDataGatewayImplementationConfiguration> result;
            using (new SystemAuthenticationContextScope(
                built.GetRequiredService<IAuthenticationContextAccessor>()))
            {
#pragma warning disable VSTHRD002
                result = domainProvider.Get("DataGateway").GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            }

            if (result.IsFailure || result.Value is null)
            {
                return result.ToNewResult<IHostApplicationBuilder>();
            }

            // The domain provider hands back the implementation the DataGateway row names.
            if (result.Value is not MainDataGatewayConfiguration configuration)
            {
                var log = loggerFactory?.CreateLogger<MainDataGatewayImplementation>()
                    ?? NullLogger<MainDataGatewayImplementation>.Instance;
                return GenericResult<IHostApplicationBuilder>.Failure(
                    DataGatewayProviderLog.ConfigurationTypeMismatch(
                        log, result.Value.GetType().Name));
            }

            builder.Services.AddSingleton(configuration);


            builder.Services.TryAddScoped<ISchemaInformationService, SchemaInformationService>();

            // FDW-5664: the option rows of a container, in the options table the server settings name, read and written through this gateway.
            builder.Services.TryAddScoped<IDataOptionRows, DataOptionRowService>();
            DataOptionRowsLog.Registered(loggerFactory?.CreateLogger<DataOptionRowService>() ?? NullLogger<DataOptionRowService>.Instance);

            // FDW-6345: the data configuration rules and their values, in the tables the settings name, beside the option rows above.
            builder.Services.TryAddScoped<IDataConfigurationRules, DataConfigurationRuleService>();
            builder.Services.TryAddScoped<IDataConfigurationRuleNames, DataConfigurationRuleNames>();
            DataOptionRulesLog.Registered(loggerFactory?.CreateLogger<DataConfigurationRuleService>() ?? NullLogger<DataConfigurationRuleService>.Instance);

            // FDW-5666: the column values search reads containers through this gateway, with the page sizes this option declares.
            var columnValuesLog = loggerFactory?.CreateLogger<ContainerColumnValues>() ?? NullLogger<ContainerColumnValues>.Instance;
            var columnValuesPaging = new ColumnValuesPaging(ColumnValuesDefaultTake, ColumnValuesMaximumTake);
            if (!columnValuesPaging.IsUsable)
                return GenericResult<IHostApplicationBuilder>.Failure(ColumnValuesLog.PagingSettingsInvalid(columnValuesLog, ColumnValuesDefaultTake, ColumnValuesMaximumTake));

            builder.Services.TryAddSingleton(columnValuesPaging);
            builder.Services.TryAddScoped<IContainerColumnValues, ContainerColumnValues>();
            ColumnValuesLog.Registered(columnValuesLog, ColumnValuesDefaultTake, ColumnValuesMaximumTake);

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

    /// <summary>
    /// Gets or sets the number of values a column values search answers when the request states none (FDW-5666). Declared here as
    /// this option's own value; a host may change it at registration.
    /// </summary>
    public int ColumnValuesDefaultTake { get; set; }

    /// <summary>
    /// Gets or sets the most values one column values page may hold (FDW-5666); a request above it is refused, never cut. Declared
    /// here as this option's own value; a host may change it at registration.
    /// </summary>
    public int ColumnValuesMaximumTake { get; set; }
}
