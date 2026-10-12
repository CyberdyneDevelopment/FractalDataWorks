using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Abstract base class for creating a one-time set-password link for a user
/// (POST /users/{IdOrName}/password-link, FDW-941).
/// </summary>
/// <remarks>
/// <para>
/// The link is returned so an administrator can share it however suits them, and when the request asks
/// it is also emailed to the user. Whoever opens it can set the user's password once, until it expires;
/// a newer link for the same user replaces it.
/// </para>
/// <para>
/// Only the platform administrator may do this, exactly as for <see cref="ResetPasswordEndpointBase"/>:
/// holding the link is holding the right to set the password. The same sign-in admission check applies,
/// because a password no sign-in flow accepts is a credential that should not exist (FDW-910).
/// </para>
/// </remarks>
public abstract class CreatePasswordLinkEndpointBase : Endpoint<CreatePasswordLinkRequest, PasswordLinkResponse>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IPasswordLinkService _linkService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePasswordLinkEndpointBase"/> class.
    /// </summary>
    /// <param name="userProvider">The user configuration provider.</param>
    /// <param name="linkService">Creates and stores the link.</param>
    /// <param name="logger">The logger.</param>
    protected CreatePasswordLinkEndpointBase(
        UserConfigurationProvider userProvider,
        IPasswordLinkService linkService,
        ILogger logger)
    {
        _userProvider = userProvider;
        _linkService = linkService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:write".
    /// </summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/users/{IdOrName}/password-link");
        Policies(WritePolicy);
        Summary(s =>
        {
            s.Summary = "Create a set-password link for a user";
            s.Description = "Creates a one-time link that lets whoever opens it set the user's password, returns it to share, and emails it to the user when asked. Platform administrator only.";
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
    public override async Task HandleAsync(CreatePasswordLinkRequest req, CancellationToken ct)
    {
        if (!Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User))
        {
            UserEndpointLog.PasswordLinkRequiresPlatformAdmin(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        try
        {
            await CreateLink(req, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            UserEndpointLog.OperationFailed(_logger, ex, "password-link", req.IdOrName);
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    private async Task CreateLink(CreatePasswordLinkRequest req, CancellationToken ct)
    {
        var userResult = Guid.TryParse(req.IdOrName, out var requestedId)
            ? await _userProvider.Get(requestedId, ct).ConfigureAwait(false)
            : await _userProvider.Get(req.IdOrName, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Credentials key on the user's durable id -- the Users domain row's.
        var user = userResult.Value;
        if (!await PasswordUsable(user.Id, req.IdOrName, ct).ConfigureAwait(false))
            return;

        if (ReadSettings() is not { } settings)
        {
            AddError("Password links are not configured on this host.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (CallerId() is not { } createdBy)
        {
            AddError("The administrator creating the link could not be identified.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var created = await _linkService
            .CreateLink(user.Id, createdBy, DateTimeOffset.UtcNow.AddMinutes(settings.LifetimeMinutes), ct)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            AddError("The password link could not be created.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.PasswordLinkCreated(_logger, user.Name, created.Value.ExpiresAt);

        // The token goes in the fragment: a browser never sends it to a server, so it stays out of
        // access logs and Referer headers.
        var response = new PasswordLinkResponse
        {
            Link = $"{settings.Url}#token={Uri.EscapeDataString(created.Value.RawToken)}",
            ExpiresAt = created.Value.ExpiresAt,
        };

        if (req.Send)
            await EmailLink(user, response, ct).ConfigureAwait(false);

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    // The same two refusals as a password reset: a password no sign-in flow accepts should not be
    // created, and an admission that cannot be decided is refused rather than assumed.
    private async Task<bool> PasswordUsable(Guid userId, string idOrName, CancellationToken ct)
    {
        var usable = await PasswordAdmission.Check(Resolve<ISignInMethodAdmission>(), userId, ct).ConfigureAwait(false);
        if (usable.IsFailure)
        {
            UserEndpointLog.PasswordAdmissionUndecided(_logger, idOrName);
            AddError("Whether the account may sign in with a password could not be decided.");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return false;
        }

        if (!usable.Value)
        {
            UserEndpointLog.PasswordNotUsable(_logger, "create a password link", idOrName);
            AddError("No enabled password sign-in flow admits this account; grant it the permission the flow requires first.");
            await Send.ErrorsAsync(409, ct).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    // The two settings a link needs. Neither has a default: a missing one is named in the log and the
    // request is refused.
    private (int LifetimeMinutes, string Url)? ReadSettings()
    {
        var settings = Resolve<IEffectiveSettingsProvider>();

        var lifetimeMinutes = settings.GetEffectiveValue<int>(SettingDefinitions.PasswordLinkLifetimeMinutes);
        if (lifetimeMinutes <= 0)
        {
            UserEndpointLog.PasswordLinkSettingMissing(_logger, SettingDefinitions.PasswordLinkLifetimeMinutes);
            return null;
        }

        var url = settings.GetEffectiveValue<string>(SettingDefinitions.PasswordLinkUrl);
        if (string.IsNullOrWhiteSpace(url))
        {
            UserEndpointLog.PasswordLinkSettingMissing(_logger, SettingDefinitions.PasswordLinkUrl);
            return null;
        }

        return (lifetimeMinutes, url);
    }

    // The administrator making the request, recorded on the link as who allowed it.
    private Guid? CallerId() =>
        Resolve<IAuthenticationContextAccessor>().Current is { } caller && Guid.TryParse(caller.UserId, out var callerId)
            ? callerId
            : null;

    // Emails the link to the user. A failure here does not undo the link: the administrator still has it
    // and can share it another way, so the response says why it was not sent.
    private async Task EmailLink(IUserImplementationConfiguration user, PasswordLinkResponse response, CancellationToken ct)
    {
        var sent = await SendEmail(user, response, ct).ConfigureAwait(false);
        response.Sent = sent.IsSuccess;
        if (sent.IsFailure)
            response.NotSentReason = sent.CurrentMessage;
    }

    private async Task<IGenericResult> SendEmail(IUserImplementationConfiguration user, PasswordLinkResponse response, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            return GenericResult.Failure(UserEndpointLog.PasswordLinkNotSent(_logger, user.Name, "the user has no email address"));

        // Why a setting: the channel is configured data, as a notification rule's is; no name is assumed.
        var channel = Resolve<IEffectiveSettingsProvider>().GetEffectiveValue<string>(SettingDefinitions.PasswordLinkNotificationChannel);
        if (string.IsNullOrWhiteSpace(channel))
            return GenericResult.Failure(UserEndpointLog.PasswordLinkNotSent(_logger, user.Name, $"the setting '{SettingDefinitions.PasswordLinkNotificationChannel}' is not set"));

        if (TryResolve<INotificationDispatcher>() is not { } dispatcher)
            return GenericResult.Failure(UserEndpointLog.PasswordLinkNotSent(_logger, user.Name, "this host has no notification dispatcher"));

        var sent = await dispatcher
            .Send(
                new NotificationRequest(
                    channel,
                    [user.Email],
                    "Set your password",
                    string.Create(CultureInfo.InvariantCulture, $"An administrator created a link for you to set your password:{Environment.NewLine}{Environment.NewLine}{response.Link}{Environment.NewLine}{Environment.NewLine}It works once, until {response.ExpiresAt:u}. If you did not expect it, ignore this message.")),
                ct)
            .ConfigureAwait(false);
        if (sent.IsFailure)
            return GenericResult.Failure(UserEndpointLog.PasswordLinkNotSent(_logger, user.Name, "the email channel did not send it"));

        UserEndpointLog.PasswordLinkSent(_logger, user.Name);
        return GenericResult.Success();
    }
}
