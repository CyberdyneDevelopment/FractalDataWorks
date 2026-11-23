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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Says who a schedule's fire acts as (FDW-6144): it reads the schedule's row (the tenant's row-level security filters it), checks that the fire is for the pipeline and tenant the schedule has, reads
/// the grant the schedule stores, and refuses a grant that has ended, a grant that allows less than the run's roles carry, and a person who may no longer act. The person is judged by the judgment
/// a run that starts uses (<c>PipelineRunPerson.Judge</c>), so a person is never judged by two rules.
/// </summary>
public sealed class ScheduleRunAs : IScheduleRunAs
{
    private const string PathName = "sched";
    private const string ContainerName = "ScheduleConfiguration";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IRunGrants _grants;
    private readonly IServiceProvider _services;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ScheduleRunAs"/> class.</summary>
    /// <param name="gateways">Gives the platform's configuration gateway.</param>
    /// <param name="grants">Reads the schedule's grant.</param>
    /// <param name="services">The services the person is judged with: the eligibility check, the tenant provider, the person's permissions and the role grants.</param>
    /// <param name="time">The clock the grant's end is judged against.</param>
    /// <param name="logger">The logger.</param>
    public ScheduleRunAs(IConfigurationGatewayProvider gateways, IRunGrants grants, IServiceProvider services, TimeProvider time, ILogger<ScheduleRunAs>? logger = null)
    {
        _gateways = gateways;
        _grants = grants;
        _services = services;
        _time = time;
        _logger = logger ?? NullLogger<ScheduleRunAs>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ScheduledRunPerson>> Resolve(ScheduledFire fire, CancellationToken cancellationToken = default)
    {
        var read = await ReadSchedule(fire, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } schedule)
        {
            return read.ToNewResult<ScheduledRunPerson>();
        }

        if (schedule.PipelineId != fire.PipelineId || schedule.TenantId != fire.TenantId)
        {
            return GenericResult<ScheduledRunPerson>.Failure(ScheduleRunAsLog.ScheduleGrantMismatch(
                _logger, fire.ScheduleId, fire.ExecutionId, schedule.PipelineId, schedule.TenantId, fire.PipelineName, fire.TenantId));
        }

        // Why a schedule with no grant is no refusal: its runs act as its tenant, as every schedule's always have, and no person is named.
        if (schedule.RunGrantId is not { } grantId)
        {
            return GenericResult<ScheduledRunPerson>.Success(new ScheduledRunPerson(null));
        }

        return await Judge(fire, grantId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<ScheduledRunPerson>> Judge(ScheduledFire fire, Guid grantId, CancellationToken cancellationToken)
    {
        var read = await _grants.Read(grantId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } grant)
        {
            // Why one refusal for both: the grant service's own message says whether the grant is not there or could not be read, and the schedule's fire is refused either way.
            return GenericResult<ScheduledRunPerson>.Failure(ScheduleRunAsLog.ScheduleGrantNotRead(_logger, fire.ScheduleId, grantId, fire.ExecutionId, read.CurrentMessage));
        }

        if (grant.HasEnded(_time.GetUtcNow()))
        {
            return GenericResult<ScheduledRunPerson>.Failure(ScheduleRunAsLog.ScheduleGrantExpired(
                _logger, fire.ScheduleId, grantId, fire.ExecutionId, grant.IsRevoked ? "was revoked" : $"ended at {grant.ExpiresAt:O}"));
        }

        var carried = await PipelineRunPerson.Judge(
            _services, new RunPersonSubject(fire.ExecutionId, fire.PipelineName, fire.RunRoles), grant.PersonId, fire.TenantId, _logger, cancellationToken).ConfigureAwait(false);
        if (carried.IsFailure || carried.Value is not { } permissions)
        {
            return carried.ToNewResult<ScheduledRunPerson>();
        }

        var notAllowed = grant.NotAllowed(permissions);
        if (notAllowed.Count > 0)
        {
            return GenericResult<ScheduledRunPerson>.Failure(ScheduleRunAsLog.ScheduleGrantNotCovering(
                _logger, fire.ScheduleId, grantId, fire.ExecutionId, string.Join(", ", notAllowed)));
        }

        ScheduleRunAsLog.ActsAsPerson(_logger, fire.ScheduleId, fire.PipelineName, fire.ExecutionId, grant.PersonId, grantId);
        return GenericResult<ScheduledRunPerson>.Success(new ScheduledRunPerson(grant.PersonId));
    }

    private async Task<IGenericResult<ScheduleGrantLinkRecord>> ReadSchedule(ScheduledFire fire, CancellationToken cancellationToken)
    {
        var rows = await Gateway().Execute<IEnumerable<ScheduleGrantLinkRecord>>(
            Query.From<ScheduleGrantLinkRecord>(PlatformConnection.Name, PathName, ContainerName).Where("Id", fire.ScheduleId).Where("IsCurrent", true).Where("IsDeleted", false).Build(), cancellationToken)
            .ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            return GenericResult<ScheduleGrantLinkRecord>.Failure(ScheduleRunAsLog.ScheduleNotRead(_logger, fire.ScheduleId, fire.ExecutionId, rows.CurrentMessage));
        }

        return rows.Value.FirstOrDefault() is { } schedule
            ? GenericResult<ScheduleGrantLinkRecord>.Success(schedule)
            : GenericResult<ScheduleGrantLinkRecord>.Failure(ScheduleRunAsLog.ScheduleNotFound(_logger, fire.ScheduleId, fire.PipelineName, fire.ExecutionId));
    }

    private IDataGateway Gateway()
    {
        var found = _gateways.Get(PlatformConnection.Name);
        return found.IsSuccess && found.Value is { } gateway
            ? gateway
            : throw new InvalidOperationException($"No configuration gateway is available for '{PlatformConnection.Name}'; it must be declared in the host's configurationSchema.json, with the table {PathName}.{ContainerName}.");
    }
}
