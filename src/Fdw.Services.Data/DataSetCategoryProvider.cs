using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Loads <c>data.DataSetCategory</c> rows at startup and registers them into
/// <see cref="DataSetCategories"/> via <c>RegisterMember</c>.
/// Handles the DB side of the Model C (hybrid) TypeCollection pattern: compile-time
/// <c>[TypeOption]</c> categories are already present in the collection when this runs;
/// this provider adds deployment-specific runtime categories on top.
/// </summary>
public sealed class DataSetCategoryProvider
{
    private const string DataStoreName = "ConfigurationDb";
    private const string PathName = "data";
    private const string ContainerName = "DataSetCategory";

    // ============================================================
    // Static DI Orchestration (three-phase)
    // ============================================================

    /// <summary>
    /// Phase 1a: No options binding required — categories come from the DB, not from appSettings.
    /// Included for API consistency with other providers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    public static void Configure(IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {
    }

    /// <summary>
    /// Phase 1b: Registers <see cref="DataSetCategoryProvider"/> as a singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    public static void Register(IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {
        services.TryAddSingleton<DataSetCategoryProvider>(sp =>
            new DataSetCategoryProvider(
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                sp.GetService<ILogger<DataSetCategoryProvider>>()));
    }

    /// <summary>
    /// Phase 2: Queries <c>data.DataSetCategory</c> and registers rows into <see cref="DataSetCategories"/>.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="loggerFactory">Optional logger factory for startup diagnostics.</param>
    public static void Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null)
    {
        var provider = services.GetRequiredService<DataSetCategoryProvider>();
#pragma warning disable VSTHRD002 // three-phase Initialize is sync-by-contract; the gatewayProvider query is a one-shot startup load
        provider.LoadAndRegister(CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    // ============================================================
    // Instance
    // ============================================================

    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DataSetCategoryProvider"/>.
    /// </summary>
    /// <param name="gatewayProvider">Supplies the gateway onto the named connection.</param>
    /// <param name="logger">Optional logger; falls back to NullLogger.</param>
    public DataSetCategoryProvider(
        IConfigurationGatewayProvider gatewayProvider,
        ILogger<DataSetCategoryProvider>? logger)
    {
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        _logger = logger ?? NullLogger<DataSetCategoryProvider>.Instance;
    }

    // ============================================================
    // Private Implementation
    // ============================================================

    private async Task LoadAndRegister(CancellationToken cancellationToken)
    {
        DataSetCategoryProviderLog.InitializeStarted(_logger);

        IGenericResult<IEnumerable<DataSetCategoryConfiguration>> result;

        try
        {
            var command = new QueryCommandBuilder<DataSetCategoryConfiguration>(DataStoreName, PathName, ContainerName)
                .Where("IsCurrent", true)
                .Where("IsDeleted", false)
                .OrderBy("SortOrder")
                .Build();

            var gateway = _gatewayProvider.Get(DataStoreName);
            if (gateway.IsFailure)
            {
                DataSetCategoryProviderLog.GatewayUnavailable(_logger, DataStoreName);
                return;
            }

            result = await gateway.Value!.Execute<IEnumerable<DataSetCategoryConfiguration>>(command, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            DataSetCategoryProviderLog.LoadCancelled(_logger, ex);
            return;
        }
        catch (Exception ex)
        {
            DataSetCategoryProviderLog.LoadFailed(_logger, ex.Message);
            return;
        }

        if (!result.IsSuccess)
        {
            DataSetCategoryProviderLog.LoadFailed(_logger, result.CurrentMessage ?? "Gateway returned failure");
            return;
        }

        var rows = result.Value?.ToList() ?? [];
        var registered = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                DataSetCategoryProviderLog.SkippingBlankName(_logger, row.Id);
                continue;
            }

            var existing = DataSetCategories.ByName(row.Name);
            if (!ReferenceEquals(existing, DataSetCategories.NotFound))
            {
                DataSetCategoryProviderLog.AlreadyRegistered(_logger, row.Name);
                continue;
            }

            DataSetCategories.RegisterMember(new RuntimeDataSetCategory(row));
            registered++;
            DataSetCategoryProviderLog.CategoryRegistered(_logger, row.Name);
        }

        DataSetCategoryProviderLog.Initialized(_logger, registered);
    }
}
