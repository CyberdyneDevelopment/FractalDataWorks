using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Abstract base class for getting user preferences (GET /users/me/preferences).
/// </summary>
public abstract class GetUserPreferencesEndpointBase : EndpointWithoutRequest<UserPreferencesResponse>
{
    private readonly UserPreferenceConfigurationProvider _preferenceProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserPreferencesEndpointBase"/> class.
    /// </summary>
    /// <param name="preferenceProvider">The user preference configuration provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected GetUserPreferencesEndpointBase(
        UserPreferenceConfigurationProvider preferenceProvider,
        ILoggerFactory loggerFactory)
    {
        _preferenceProvider = preferenceProvider;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets the user preference configuration provider.
    /// </summary>
    protected UserPreferenceConfigurationProvider PreferenceProvider => _preferenceProvider;

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/users/me/preferences");
        Summary(s =>
        {
            s.Summary = "Get user preferences";
            s.Description = "Returns the current user's preferences.";
        });
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!PreferenceEndpointIdentity.TryGetUserId(User, out var userId))
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var result = await _preferenceProvider.GetPreferences(userId, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                AddError(result.CurrentMessage ?? "Failed to get preferences.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var prefs = result.Value;
            var response = prefs is not null
                ? new UserPreferencesResponse
                {
                    ThemeName = prefs.ThemeName,
                    DarkMode = prefs.DarkMode,
                    Language = prefs.Language,
                    Timezone = prefs.Timezone
                }
                : new UserPreferencesResponse { DarkMode = false };
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            UserEndpointLog.GetPreferencesFailed(_logger, ex, userId.ToString());
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
