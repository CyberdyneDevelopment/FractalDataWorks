using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to retrieve the current default theme configuration.
/// Subclasses must implement <see cref="LoadDefaultTheme"/> to provide the theme data. A failed result is answered
/// with the status its message carries, so no default being set answers 404 and a failed read answers its own status.
/// </summary>
/// <typeparam name="TDetail">The theme detail DTO type.</typeparam>
public abstract class GetDefaultThemeEndpointBase<TDetail> : EndpointWithoutRequest<TDetail>
    where TDetail : class
{
    /// <summary>Initializes a new instance of the <see cref="GetDefaultThemeEndpointBase{TDetail}"/> class.</summary>
    protected GetDefaultThemeEndpointBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>Gets the resource name used for routing and policies.</summary>
    protected virtual string ResourceName => "themes";

    /// <summary>Gets the authorization policy name for read operations.</summary>
    protected virtual string ReadPolicy => "configurations:read";

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected new ILogger Logger { get; }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get($"/{ResourceName}/default");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Get default {ResourceName}";
            s.Description = $"Returns the current default {ResourceName} configuration.";
        });
    }

    /// <summary>Returns the current default theme configuration, or the status of the failure to find it.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var theme = await LoadDefaultTheme(ct).ConfigureAwait(false);
        if (!theme.IsSuccess || theme.Value is not { } found)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(theme, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(found, ct).ConfigureAwait(false);
    }

    /// <summary>Loads the default theme. Implemented by subclasses to provide theme data.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The stored default theme; a failure when none is set or the read fails.</returns>
    protected abstract Task<IGenericResult<TDetail>> LoadDefaultTheme(CancellationToken ct);
}
