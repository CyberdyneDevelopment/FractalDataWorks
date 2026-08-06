using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Clients;

/// <summary>
/// HTTP client implementation for the scheduling service.
/// </summary>
public class ScheduleHttpClient : ApiClientBase, IScheduleClient, IResourceQueryClient<ScheduleInfoDto, ScheduleInfoDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleHttpClient"/> class.
    /// </summary>
    public ScheduleHttpClient(HttpClient httpClient, ILogger<ScheduleHttpClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<ScheduleTypeSummary>>> GetScheduleTypes(CancellationToken cancellationToken = default)
        // Why: endpoint returns a flat JSON array; GetList handles both array and paginated-envelope formats.
        => GetList<ScheduleTypeSummary>("schedules/types", cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<ScheduleInfoDto>>> List(CancellationToken cancellationToken = default)
    {
        // Why: CrudListEndpoint emits the paged envelope { items: [...], totalCount } —
        // use GetList which unwraps both the envelope and a plain array; Get<IReadOnlyList<T>>
        // can't unwrap the envelope and always yields an empty list (FINDINGS bug #2).
        return GetList<ScheduleInfoDto>("schedules", cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<ScheduleInfoDto>> Get(string name, CancellationToken cancellationToken = default)
    {
        return Get<ScheduleInfoDto>($"schedules/{name}", cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<CreateScheduleClientResponse>> CreateSchedule(
        CreateScheduleClientRequest request, CancellationToken cancellationToken = default)
    {
        return Post<CreateScheduleClientRequest, CreateScheduleClientResponse>("schedules", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> UpdateSchedule(
        string name, UpdateScheduleClientRequest request, CancellationToken cancellationToken = default)
    {
        return Put<UpdateScheduleClientRequest>($"schedules/{name}", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> DeleteSchedule(string name, CancellationToken cancellationToken = default)
    {
        return Delete($"schedules/{name}", cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<ScheduleInfoDto>> ToggleSchedule(string name, CancellationToken cancellationToken = default)
    {
        return Post<object, ScheduleInfoDto>($"schedules/{name}/toggle", new { }, cancellationToken);
    }
}
