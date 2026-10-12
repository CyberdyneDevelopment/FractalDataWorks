using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Agents.Clients.Models;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Agents.Clients;

/// <summary>
/// API client for agent action review queue endpoints.
/// </summary>
public class AgentActionClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentActionClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public AgentActionClient(HttpClient httpClient, ILogger<AgentActionClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Lists all agent actions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of agent actions.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<AgentActionPayload>>> List(CancellationToken ct = default)
        => GetList<AgentActionPayload>("agent-actions", ct);

    /// <summary>
    /// Gets a single agent action by identifier.
    /// </summary>
    /// <param name="actionId">The action identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the agent action.</returns>
    public virtual Task<IGenericResult<AgentActionPayload>> Get(int actionId, CancellationToken ct = default)
        => Get<AgentActionPayload>($"agent-actions/{actionId}", ct);

    /// <summary>
    /// Approves a pending agent action.
    /// </summary>
    /// <param name="actionId">The action identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public virtual Task<IGenericResult> Approve(int actionId, CancellationToken ct = default)
        => Post($"agent-actions/{actionId}/approve", ct);

    /// <summary>
    /// Denies a pending agent action.
    /// </summary>
    /// <param name="actionId">The action identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public virtual Task<IGenericResult> Deny(int actionId, CancellationToken ct = default)
        => Post($"agent-actions/{actionId}/deny", ct);
}
