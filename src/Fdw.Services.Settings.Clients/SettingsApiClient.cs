namespace Fdw.Services.Settings.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for server settings operations.
/// </summary>
public class SettingsApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public SettingsApiClient(HttpClient httpClient, ILogger<SettingsApiClient> logger)
        : base(httpClient, logger) { }

    /// <summary>
    /// Lists all server settings.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the list of server settings.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ServerSettingResponse>>> List(CancellationToken ct = default)
        => GetList<ServerSettingResponse>("settings/server", ct);

    /// <summary>
    /// Gets a server setting by name.
    /// </summary>
    /// <param name="settingName">The setting name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the server setting.</returns>
    public virtual Task<IGenericResult<ServerSettingResponse>> Get(string settingName, CancellationToken ct = default)
        => Get<ServerSettingResponse>($"settings/server/{Uri.EscapeDataString(settingName)}", ct);

    /// <summary>
    /// Updates a server setting.
    /// </summary>
    /// <param name="settingName">The setting name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the updated server setting.</returns>
    public virtual Task<IGenericResult<ServerSettingResponse>> Update(string settingName, UpdateServerSettingPayload request, CancellationToken ct = default)
        => Put<UpdateServerSettingPayload, ServerSettingResponse>(
            $"settings/server/{Uri.EscapeDataString(settingName)}", request, ct);
}
