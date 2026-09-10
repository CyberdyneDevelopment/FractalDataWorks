using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>Supplies the EtlPipeline configuration.</summary>
/// <remarks>
/// Both halves of one record: the EtlPipeline domain (its engines are the implementations it
/// dispatches to) and the Pipeline domain's "Etl" implementation (<c>pipe.EtlPipeline</c> hangs from
/// <c>pipe.Pipeline</c>). The Pipeline-facing surface is the same read, narrowed to the contract that
/// domain hands back -- which every EtlPipeline implementation carries.
/// </remarks>
public sealed class EtlPipelineConfigurationProvider
    : DomainConfigurationProviderBase<IEtlPipelineImplementationConfiguration>,
      IImplementationConfigurationProvider<IPipelineImplementationConfiguration>
{
    private readonly ILogger<EtlPipelineConfigurationProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="EtlPipelineConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public EtlPipelineConfigurationProvider(
        ILogger<EtlPipelineConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "pipe", "EtlPipeline")
    {
        _logger = logger ?? NullLogger<EtlPipelineConfigurationProvider>.Instance;
    }

    // ── the Pipeline domain's contract ──────────────────────────────────────
    // What Pipeline registers this under. Explicit because the two contracts share these signatures
    // and differ only in return type; each read is this domain's own, handed back as the narrower
    // contract every EtlPipeline implementation already carries.

    async Task<IGenericResult<IPipelineImplementationConfiguration>> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Get(
        Guid domainId, CancellationToken cancellationToken)
    {
        var found = await Get(domainId, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IPipelineImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IPipelineImplementationConfiguration>();
    }

    async Task<IGenericResult<IPipelineImplementationConfiguration>> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Get(
        Guid domainId, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var found = await Get(domainId, asOf, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IPipelineImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IPipelineImplementationConfiguration>();
    }

    async Task<IGenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Get(
        IEnumerable<Guid> domainIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainIds);

        var found = new List<IPipelineImplementationConfiguration>();
        foreach (var domainId in domainIds)
        {
            var one = await Get(domainId, cancellationToken).ConfigureAwait(false);
            if (!one.IsSuccess) return one.ToNewResult<IReadOnlyList<IPipelineImplementationConfiguration>>();
            if (one.Value is not null) found.Add(one.Value);
        }

        return GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success(found);
    }

    async Task<IGenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Get(
        CancellationToken cancellationToken)
    {
        var found = await Get(cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess) return found.ToNewResult<IReadOnlyList<IPipelineImplementationConfiguration>>();

        return GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([.. found.Value!]);
    }

    async Task<IGenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Find(
        Func<IPipelineImplementationConfiguration, bool> predicate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var found = await Find<IEtlPipelineImplementationConfiguration>(
            configuration => predicate(configuration), cancellationToken).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([.. found.Value!])
            : found.ToNewResult<IReadOnlyList<IPipelineImplementationConfiguration>>();
    }

    async Task<IGenericResult<IPipelineImplementationConfiguration>> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Save(
        IPipelineImplementationConfiguration record, CancellationToken cancellationToken)
    {
        if (record is not IEtlPipelineImplementationConfiguration typed)
        {
            return GenericResult<IPipelineImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger,
                    nameof(IEtlPipelineImplementationConfiguration),
                    record?.GetType().Name ?? "(null)"));
        }

        var saved = await Save(typed, typed.Domain, typed.Implementation, typed.Name, cancellationToken)
            .ConfigureAwait(false);
        return saved.IsSuccess
            ? GenericResult<IPipelineImplementationConfiguration>.Success(typed)
            : saved.ToNewResult<IPipelineImplementationConfiguration>();
    }

    async Task<IGenericResult> IImplementationConfigurationProvider<IPipelineImplementationConfiguration>.Delete(
        Guid domainId, CancellationToken cancellationToken)
        => await Delete(domainId, cancellationToken).ConfigureAwait(false);
}
