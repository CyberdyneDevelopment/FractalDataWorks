namespace Fdw.UI.Themes.Clients.ApiClients;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Themes.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for theme management endpoints.
/// </summary>
public class ThemeApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public ThemeApiClient(HttpClient httpClient, ILogger<ThemeApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all available themes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of theme summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ThemeSummaryPayload>>> GetThemes(CancellationToken ct = default)
        => GetList<ThemeSummaryPayload>("themes", ct);

    /// <summary>
    /// Gets a theme by name.
    /// </summary>
    /// <param name="name">The theme name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the theme configuration.</returns>
    public virtual Task<IGenericResult<ThemeConfiguration>> GetTheme(string name, CancellationToken ct = default)
        => Get<ThemeConfiguration>($"themes/{name}", ct);

    /// <summary>
    /// Gets the default theme.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the default theme configuration.</returns>
    public virtual Task<IGenericResult<ThemeConfiguration>> GetDefaultTheme(CancellationToken ct = default)
        => Get<ThemeConfiguration>("themes/default", ct);

    /// <summary>
    /// Creates a new theme.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created theme configuration.</returns>
    public virtual Task<IGenericResult<ThemeConfiguration>> CreateTheme(CreateThemeRequest request, CancellationToken ct = default)
        => Post<CreateThemeRequest, ThemeConfiguration>("themes", request, ct);

    /// <summary>
    /// Updates an existing theme.
    /// </summary>
    /// <param name="name">The theme name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated theme configuration.</returns>
    public virtual Task<IGenericResult<ThemeConfiguration>> UpdateTheme(string name, UpdateThemeRequest request, CancellationToken ct = default)
        => Put<UpdateThemeRequest, ThemeConfiguration>($"themes/{name}", request, ct);

    /// <summary>
    /// Deletes a theme.
    /// </summary>
    /// <param name="name">The theme name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the deletion.</returns>
    public virtual Task<IGenericResult> DeleteTheme(string name, CancellationToken ct = default)
        => Delete($"themes/{name}", ct);

    /// <summary>
    /// Sets a theme as the default.
    /// </summary>
    /// <param name="name">The theme name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public virtual Task<IGenericResult> SetDefaultTheme(string name, CancellationToken ct = default)
        => Post($"themes/{name}/default", new { }, ct);
}
