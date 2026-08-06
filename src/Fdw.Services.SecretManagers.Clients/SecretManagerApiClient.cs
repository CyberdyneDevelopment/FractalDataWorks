namespace Fdw.Services.SecretManagers.Clients;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Services.SecretManagers.Clients.Models;
using Fdw.Web.Clients.Abstractions;

/// <summary>
/// API client for secret manager endpoints.
/// </summary>
public class SecretManagerApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public SecretManagerApiClient(HttpClient httpClient, ILogger<SecretManagerApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all configured secret managers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of secret manager summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<SecretManagerSummaryPayload>>> GetSecretManagers(CancellationToken ct = default)
        => GetList<SecretManagerSummaryPayload>("secret-managers", ct);

    /// <summary>
    /// Gets all available secret manager types registered on the server.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of secret manager type summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<SecretManagerTypeSummaryPayload>>> GetSecretManagerTypes(CancellationToken ct = default)
        => GetList<SecretManagerTypeSummaryPayload>("secret-manager-types", ct);

    /// <summary>
    /// Gets a specific secret manager by name.
    /// </summary>
    /// <param name="name">The secret manager name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the secret manager details.</returns>
    public virtual Task<IGenericResult<SecretManagerDetailPayload>> GetSecretManager(string name, CancellationToken ct = default)
        => Get<SecretManagerDetailPayload>($"secret-managers/{name}", ct);

    /// <summary>
    /// Creates a new secret manager configuration.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created secret manager details.</returns>
    public virtual Task<IGenericResult<SecretManagerDetailPayload>> CreateSecretManager(CreateSecretManagerPayload request, CancellationToken ct = default)
        => Post<CreateSecretManagerPayload, SecretManagerDetailPayload>("secret-managers", request, ct);

    /// <summary>
    /// Updates an existing secret manager configuration.
    /// </summary>
    /// <param name="name">The secret manager name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated secret manager details.</returns>
    public virtual Task<IGenericResult<SecretManagerDetailPayload>> UpdateSecretManager(string name, UpdateSecretManagerPayload request, CancellationToken ct = default)
        => Put<UpdateSecretManagerPayload, SecretManagerDetailPayload>($"secret-managers/{name}", request, ct);

    /// <summary>
    /// Deletes a secret manager configuration.
    /// </summary>
    /// <param name="name">The secret manager name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public virtual Task<IGenericResult> DeleteSecretManager(string name, CancellationToken ct = default)
        => Delete($"secret-managers/{name}", ct);

    /// <summary>
    /// Gets the names of all configured secret managers (legacy endpoint).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of manager names.</returns>
    public virtual Task<IGenericResult<ListSecretManagersResponse>> GetManagers(CancellationToken ct = default)
        => Get<ListSecretManagersResponse>("secrets", ct);

    /// <summary>
    /// Stores a secret in the named secret manager.
    /// </summary>
    /// <param name="managerName">The name of the secret manager (e.g. "AzureKeyVault").</param>
    /// <param name="keyName">The key name to store the secret under.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the response with the stored key name and success status.</returns>
    public virtual Task<IGenericResult<SetSecretResponse>> SetSecret(
        string managerName,
        string keyName,
        string value,
        CancellationToken ct = default)
        => Post<SetSecretRequest, SetSecretResponse>(
            $"secrets/{managerName}",
            new SetSecretRequest { KeyName = keyName, Value = value },
            ct);
}
