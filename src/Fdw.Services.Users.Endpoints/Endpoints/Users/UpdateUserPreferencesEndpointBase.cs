using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Abstract base class for updating user preferences (PUT /users/me/preferences).
/// </summary>
public abstract class UpdateUserPreferencesEndpointBase : Endpoint<UpdateUserPreferencesRequest>
{
    private readonly UserPreferenceConfigurationProvider _preferenceProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserPreferencesEndpointBase"/> class.
    /// </summary>
    /// <param name="preferenceProvider">The user preference configuration provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected UpdateUserPreferencesEndpointBase(
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
        Patch("/users/me/preferences");
        Summary(s =>
        {
            s.Summary = "Update user preferences";
            s.Description = "Sets or updates preferences for the current user.";
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
    public override async Task HandleAsync(UpdateUserPreferencesRequest req, CancellationToken ct)
    {
        // Why: the FDW access token carries the durable user GUID in the standard JWT `sub`
        // claim (ClaimDefinitions defines no `name`/`preferred_username` claim), so
        // User.Identity.Name is empty and resolving identity from it 401s every authenticated
        // caller. Read `sub` directly — the canonical pattern the SessionState endpoints use —
        // and fail loud (401) when it is absent or not a GUID. No username round-trip, no fallback.
        if (!PreferenceEndpointIdentity.TryGetUserId(User, out var userId))
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        try
        {
            // Why: load the current row (if any) so we can patch only the supplied fields and
            // preserve the rest. This is a typed record — there is no per-field SetPreference.
            var loadResult = await _preferenceProvider.GetPreferences(userId, ct).ConfigureAwait(false);
            if (!loadResult.IsSuccess)
            {
                AddError(loadResult.CurrentMessage ?? "Failed to load existing preferences.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var existing = loadResult.Value;
            var record = existing is not null
                ? existing
                : new UserPreferencesConfiguration
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    IsCurrent = true,
                    IsDeleted = false,
                };

            // Why: only patch fields the caller supplied (non-null). Fields omitted from the
            // request retain their stored values.
            if (req.ThemeName is not null) record.ThemeName = req.ThemeName;
            if (req.DarkMode.HasValue) record.DarkMode = req.DarkMode.Value;
            if (req.Language is not null) record.Language = req.Language;
            if (req.Timezone is not null) record.Timezone = req.Timezone;

            var saveResult = await _preferenceProvider.Save(record, ct).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
            {
                AddError(saveResult.CurrentMessage ?? "Failed to save preferences.");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            UserEndpointLog.UpdatePreferencesFailed(_logger, ex, userId.ToString());
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
