using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Abstract base class for getting user preferences (GET /users/me/preferences).
/// </summary>
public abstract class GetUserPreferencesEndpointBase : EndpointWithoutRequest<UserPreferencesResponse>
{
    private readonly IUserPreferenceConfigurationProvider _preferenceProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserPreferencesEndpointBase"/> class.
    /// </summary>
    /// <param name="preferenceProvider">The user preference configuration provider.</param>
    /// <param name="logger">The logger.</param>
    protected GetUserPreferencesEndpointBase(
        IUserPreferenceConfigurationProvider preferenceProvider,
        ILogger logger)
    {
        _preferenceProvider = preferenceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the user preference configuration provider.
    /// </summary>
    protected IUserPreferenceConfigurationProvider PreferenceProvider => _preferenceProvider;

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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.GetUserPreferencesNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        try
        {
            var result = await _preferenceProvider.Get(PreferenceEndpointIdentity.RowName(userId), ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
                return;
            }

            var prefs = result.Value;
            var response = prefs is not null
                ? new UserPreferencesResponse
                {
                    ThemeName = prefs.ThemeName,
                    DarkMode = prefs.DarkMode,
                    ColorMode = prefs.ColorMode,
                    Language = prefs.Language,
                    Timezone = prefs.Timezone
                }
                : new UserPreferencesResponse();
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            UserEndpointLog.GetPreferencesFailed(_logger, ex, userId.ToString());
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(UserEndpointLog.GetUserPreferencesFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }
}
