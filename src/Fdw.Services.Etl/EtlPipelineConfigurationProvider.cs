using System;
using Fdw.Configuration;
using Fdw.Services.Results;
using Fdw.Results;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Etl;

/// <summary>
/// The ETL pipeline layer: an implementation of the Pipeline domain, and itself the domain for the
/// engines it names (BatchCopy, Streaming).
/// </summary>
/// <remarks>
/// This is the layered case <c>IServiceDispatchHost</c> describes. Pipeline names "Etl"; the Etl row
/// then names the engine, and the factories are registered under that deeper name. So this provider
/// owns a registry of its own — it is a domain provider — while still satisfying the Pipeline
/// domain's implementation contract so the Pipeline provider can register it.
/// </remarks>
public class EtlPipelineConfigurationProvider
    : ImplementationConfigurationProviderBase<EtlPipelineConfiguration, IEtlPipelineImplementationConfiguration, EtlPipelineConfigurationCommand>,
      IImplementationConfigurationProvider<IPipelineImplementationConfiguration>
{


    /// <summary>Initializes a new instance of the <see cref="EtlPipelineConfigurationProvider"/> class.</summary>
    public EtlPipelineConfigurationProvider(
        ILogger<EtlPipelineConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "pipe")
        : base(logger ?? NullLogger<EtlPipelineConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    async Task<IGenericResult<IPipelineImplementationConfiguration>>
        IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Get(
            Guid domainId, CancellationToken cancellationToken)
        => await Get(domainId, cancellationToken).ConfigureAwait(false);

    async Task<IGenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>>
        IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Get(
            CancellationToken cancellationToken)
    {
        var all = await Get(cancellationToken).ConfigureAwait(false);
        return all.IsSuccess && all.Value is not null
            ? GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success(
                (IReadOnlyList<IPipelineImplementationConfiguration>)all.Value)
            : all.ToNewResult<IReadOnlyList<IPipelineImplementationConfiguration>>();
    }

    async Task<IGenericResult<IPipelineImplementationConfiguration>>
        IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Save(
            IPipelineImplementationConfiguration record, CancellationToken cancellationToken)
        => record is EtlPipelineConfiguration typed
            ? await Save(typed, cancellationToken).ConfigureAwait(false)
            : GenericResult<IPipelineImplementationConfiguration>.Failure(
                ServicesResultCodes.ByName("InvalidConfigurationType"),
                ResultDetails.Create("ExpectedType", nameof(EtlPipelineConfiguration),
                                     "ActualType", record?.GetType().Name ?? "(null)"));

    Task<IGenericResult> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Delete(
        Guid domainId, CancellationToken cancellationToken)
        => Delete(domainId, cancellationToken);
}
