using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The offers of pipelines (FDW-6135): the pipeline's own configuration in <c>pipe.PipelineOffer</c>, a child record written and retired through the configuration save and retire path and read through
/// the platform's configuration gateway, in the caller's tenant (the row-level security of the tenant filters what is read). It reads for the run starter (<see cref="IPipelineOffers"/>) and lists, adds
/// and removes for the routes (<see cref="IPipelineOfferService"/>). A pipeline is offered when an offer of the whole tenant, of the person or of one of the roles stands; none stands, none is assumed.
/// </summary>
public sealed class PipelineOffers : IPipelineOffers, IPipelineOfferService
{
    private const string PathName = "pipe";
    private const string ContainerName = "PipelineOffer";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineOffers"/> class.</summary>
    /// <param name="gateways">Gives the platform's configuration gateway.</param>
    /// <param name="pipelines">Finds a pipeline, so an offer is never made for one that does not exist.</param>
    /// <param name="logger">The logger.</param>
    public PipelineOffers(IConfigurationGatewayProvider gateways, IPipelineConfigurationProvider pipelines, ILogger<PipelineOffers>? logger = null)
    {
        _gateways = gateways;
        _pipelines = pipelines;
        _logger = logger ?? NullLogger<PipelineOffers>.Instance;
    }

    private static DataStoreTarget Target => new(PlatformConnection.Name, PathName, ContainerName);

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> IsOffered(Guid pipelineId, Guid? personId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
    {
        var offers = await Read(pipelineId, cancellationToken).ConfigureAwait(false);
        return offers.IsFailure || offers.Value is null
            ? offers.ToNewResult<bool>()
            : GenericResult<bool>.Success(offers.Value.Any(offer =>
                (offer.PersonId is null && string.IsNullOrEmpty(offer.RoleName))
                || (personId is { } person && offer.PersonId == person)
                || (!string.IsNullOrEmpty(offer.RoleName) && roles.Contains(offer.RoleName, StringComparer.OrdinalIgnoreCase))));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<PipelineOffer>>> List(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        var exists = await Exists(pipelineId, cancellationToken).ConfigureAwait(false);
        if (exists.IsFailure)
        {
            return exists.ToNewResult<IReadOnlyList<PipelineOffer>>();
        }

        var offers = await Read(pipelineId, cancellationToken).ConfigureAwait(false);
        return offers.IsFailure || offers.Value is null
            ? offers.ToNewResult<IReadOnlyList<PipelineOffer>>()
            : GenericResult<IReadOnlyList<PipelineOffer>>.Success([.. offers.Value.Select(Of)]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineOffer>> Add(Guid pipelineId, Guid? personId, string? roleName, CancellationToken cancellationToken = default)
    {
        var role = string.IsNullOrWhiteSpace(roleName) ? null : roleName.Trim();
        if (personId is not null && role is not null)
        {
            return GenericResult<PipelineOffer>.Failure(PipelineOfferLog.NamesPersonAndRole(_logger, pipelineId));
        }

        var exists = await Exists(pipelineId, cancellationToken).ConfigureAwait(false);
        if (exists.IsFailure)
        {
            return exists.ToNewResult<PipelineOffer>();
        }

        var offers = await Read(pipelineId, cancellationToken).ConfigureAwait(false);
        if (offers.IsFailure || offers.Value is null)
        {
            return offers.ToNewResult<PipelineOffer>();
        }

        if (offers.Value.Any(offer => offer.PersonId == personId && string.Equals(offer.RoleName ?? string.Empty, role ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
        {
            return GenericResult<PipelineOffer>.Failure(PipelineOfferLog.AlreadyStands(_logger, pipelineId, To(personId, role)));
        }

        var row = new PipelineOfferRecord { PipelineId = pipelineId, PersonId = personId, RoleName = role };
        var saved = await Gateway().Execute<ConfigurationWriteResult>(new ConfigurationSaveCommand<PipelineOfferRecord>(row, replacesCurrent: true), Target, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? GenericResult<PipelineOffer>.Failure(PipelineOfferLog.NotWritten(_logger, pipelineId, saved.CurrentMessage))
            : GenericResult<PipelineOffer>.Success(Of(row));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Remove(Guid pipelineId, Guid offerId, CancellationToken cancellationToken = default)
    {
        var exists = await Exists(pipelineId, cancellationToken).ConfigureAwait(false);
        if (exists.IsFailure)
        {
            return exists;
        }

        var offers = await Read(pipelineId, cancellationToken).ConfigureAwait(false);
        if (offers.IsFailure || offers.Value is null)
        {
            return offers;
        }

        return offers.Value.Any(offer => offer.Id == offerId)
            ? await Retire(pipelineId, offerId, cancellationToken).ConfigureAwait(false)
            : GenericResult.Failure(PipelineOfferLog.OfferNotFound(_logger, pipelineId, offerId));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RemoveAll(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        var offers = await Read(pipelineId, cancellationToken).ConfigureAwait(false);
        if (offers.IsFailure || offers.Value is null)
        {
            return offers;
        }

        foreach (var offer in offers.Value)
        {
            var retired = await Retire(pipelineId, offer.Id, cancellationToken).ConfigureAwait(false);
            if (retired.IsFailure)
            {
                return retired;
            }
        }

        return GenericResult.Success();
    }

    private IDataGateway Gateway()
    {
        var found = _gateways.Get(PlatformConnection.Name);
        return found.IsSuccess && found.Value is { } gateway
            ? gateway
            : throw new InvalidOperationException($"No configuration gateway is available for '{PlatformConnection.Name}'; it must be declared in the host's configurationSchema.json, with the table {PathName}.{ContainerName}.");
    }

    private async Task<IGenericResult> Exists(Guid pipelineId, CancellationToken ct)
    {
        var found = await _pipelines.Get(pipelineId, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, ct).ConfigureAwait(false);
        return found.IsSuccess && found.Value is not null ? GenericResult.Success() : GenericResult.Failure(PipelineOfferLog.PipelineNotFound(_logger, pipelineId));
    }

    private async Task<IGenericResult<IReadOnlyList<PipelineOfferRecord>>> Read(Guid pipelineId, CancellationToken ct)
    {
        var rows = await Gateway().Execute<IEnumerable<PipelineOfferRecord>>(
            Query.From<PipelineOfferRecord>(PlatformConnection.Name, PathName, ContainerName).Where("PipelineId", pipelineId).Where("IsCurrent", true).Where("IsDeleted", false).Build(), ct)
            .ConfigureAwait(false);
        return rows.IsFailure || rows.Value is null
            ? GenericResult<IReadOnlyList<PipelineOfferRecord>>.Failure(PipelineOfferLog.NotRead(_logger, pipelineId, rows.CurrentMessage))
            : GenericResult<IReadOnlyList<PipelineOfferRecord>>.Success([.. rows.Value]);
    }

    private async Task<IGenericResult> Retire(Guid pipelineId, Guid offerId, CancellationToken ct)
    {
        var retired = await Gateway().Execute<ConfigurationWriteResult>(
            new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [offerId], markDeleted: true), Target, ct).ConfigureAwait(false);
        return retired.IsFailure ? GenericResult.Failure(PipelineOfferLog.NotWritten(_logger, pipelineId, retired.CurrentMessage)) : GenericResult.Success();
    }

    private static PipelineOffer Of(PipelineOfferRecord row) => new() { Id = row.Id, PipelineId = row.PipelineId, PersonId = row.PersonId, RoleName = row.RoleName };

    private static string To(Guid? personId, string? role) => personId is { } person ? $"person {person}" : role is not null ? $"role {role}" : "the whole tenant";
}
