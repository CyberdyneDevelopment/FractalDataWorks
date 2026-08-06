using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Defines the contract for interacting with the scheduling service.
/// </summary>
public interface IScheduleClient
{
    /// <summary>
    /// Gets all available schedule types from the domain's source-generated TypeCollection.
    /// </summary>
    /// <remarks>
    /// Calls GET /schedules/types. Use this instead of the generic
    /// ConfigurationApiClient.GetTypesByCategory("Schedule") which walks the schema-container
    /// tree and returns zero results for the Schedule category.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<ScheduleTypeSummary>>> GetScheduleTypes(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all configured schedules.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<ScheduleInfoDto>>> List(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schedule by name.
    /// </summary>
    Task<IGenericResult<ScheduleInfoDto>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new schedule.
    /// </summary>
    Task<IGenericResult<CreateScheduleClientResponse>> CreateSchedule(CreateScheduleClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing schedule by name.
    /// </summary>
    Task<IGenericResult> UpdateSchedule(string name, UpdateScheduleClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schedule by name.
    /// </summary>
    Task<IGenericResult> DeleteSchedule(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles a schedule's enabled state.
    /// </summary>
    Task<IGenericResult<ScheduleInfoDto>> ToggleSchedule(string name, CancellationToken cancellationToken = default);
}
