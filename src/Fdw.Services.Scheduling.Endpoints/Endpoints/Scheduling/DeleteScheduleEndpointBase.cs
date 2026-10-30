using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a schedule configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete schedule configuration type.</typeparam>
public abstract class DeleteScheduleEndpointBase<TConfig> : CrudDeleteEndpointBase<ScheduleNameRequest>
    where TConfig : class, IScheduleImplementationConfiguration
{
    private readonly ScheduleConfigurationProvider _provider;
    private readonly WaitThresholdOwnerRelease _release;

    /// <inheritdoc />
    protected DeleteScheduleEndpointBase(
        ILogger<DeleteScheduleEndpointBase<TConfig>> logger, ScheduleConfigurationProvider provider, IConfigurationGatewayProvider gateways) : base(logger)
    {
        _provider = provider;
        _release = new WaitThresholdOwnerRelease(gateways, logger);
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "schedules";

    /// <summary>Returns the schedule name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ScheduleNameRequest request) => request.Name;

    /// <summary>Checks whether the schedule exists before attempting deletion.</summary>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(ScheduleNameRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <summary>Performs the delete operation by marking the schedule as deleted via the DataGateway.</summary>
    protected override async Task<IGenericResult> Delete(ScheduleNameRequest request, CancellationToken ct)
    {
        var existingResult = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        var existing = existingResult.IsSuccess ? existingResult.Value : null;
        if (existing is null)
        {
            return GenericResult.Success();
        }

        // The schedule's own level of thresholds on waiting runs goes with it (FDW-3271).
        var released = await _release.ReleaseSchedule(request.Name, ct).ConfigureAwait(false);
        if (released.IsFailure)
        {
            return released;
        }

        var deleteResult = await _provider.Delete(existing.Id, ct).ConfigureAwait(false);
        if (!deleteResult.IsSuccess)
        {
            return GenericResult.Failure(
                ScheduleEndpointLog.DeleteFailed(Logger, request.Name));
        }

        return deleteResult;
    }
}
