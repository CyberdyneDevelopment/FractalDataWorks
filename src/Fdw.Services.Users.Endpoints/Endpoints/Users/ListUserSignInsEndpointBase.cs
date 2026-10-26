using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Fdw.Services.Users.Configuration;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint listing an account's sign-ins, newest first: when, through which service and flow, and from where (FDW-2729).
/// </summary>
/// <remarks>
/// Who may read them is <see cref="AccountAuthority.ReadsSignIns"/>: the account itself, its home-tenant administrator, or a platform
/// administrator. A page asks for one row more than it returns, which is how it knows whether older sign-ins follow without counting the log.
/// A skip below zero, or a take outside 1 to <see cref="ListUserSignInsRequest.MaxTake"/>, is refused and not corrected.
/// </remarks>
public abstract class ListUserSignInsEndpointBase : Endpoint<ListUserSignInsRequest, SignInPageResponse>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly ISignInLog _signIns;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ListUserSignInsEndpointBase"/> class.</summary>
    protected ListUserSignInsEndpointBase(ILogger logger, UserConfigurationProvider userProvider, ISignInLog signIns)
    {
        _logger = logger;
        _userProvider = userProvider;
        _signIns = signIns ?? throw new ArgumentNullException(nameof(signIns));
    }

    /// <remarks>
    /// No permission policy: a person reads their own sign-ins, and a member holds no users:read. Who may read whose is
    /// <see cref="AccountAuthority.ReadsSignIns"/>, which the handler asks.
    /// </remarks>
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/users/{IdOrName}/sign-ins");
        Summary(s =>
        {
            s.Summary = "List an account's sign-ins";
            s.Description = "Newest first: when, the service and flow, the address, the browser and the operating system. A key request is a sign-in too.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ListUserSignInsRequest req, CancellationToken ct)
    {
        if (req.Skip < 0 || req.Take < 1 || req.Take > ListUserSignInsRequest.MaxTake)
        {
            await Send.ResultAsync(TypedResults.Json(new
            {
                errorCode = "BadRequest",
                messages = new[] { $"skip must be 0 or more, and take must be from 1 to {ListUserSignInsRequest.MaxTake}." },
            }, statusCode: 400)).ConfigureAwait(false);
            return;
        }

        var userResult = await UserRouteTarget
            .Resolve(_userProvider, User, req.IdOrName, ct).ConfigureAwait(false);

        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        if (!AccountAuthority.ReadsSignIns(User, user, Resolve<ISystemRoleConfiguration>()))
        {
            UserEndpointLog.SignInsNotPermitted(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var read = await _signIns.SignIns(user.Id, req.Skip, req.Take + 1, ct).ConfigureAwait(false);
        if (read.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(read, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        var page = read.Value!.Take(req.Take).ToList();
        UserEndpointLog.SignInsListed(_logger, user.Id.ToString("D"), page.Count);

        await Send.OkAsync(new SignInPageResponse
        {
            Skip = req.Skip,
            Take = req.Take,
            HasMore = read.Value!.Count > req.Take,
            Items = page.Select(sign => new SignInResponse
            {
                SignedInAt = sign.SignedInAt,
                ServiceName = sign.ServiceName,
                FlowName = sign.FlowName,
                IpAddress = sign.IpAddress,
                Browser = sign.Browser,
                OperatingSystem = sign.OperatingSystem,
                UserAgent = sign.UserAgent,
            }).ToList(),
        }, ct).ConfigureAwait(false);
    }
}
