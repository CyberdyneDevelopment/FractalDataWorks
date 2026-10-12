using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Models;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint for getting a user by ID or username: <c>POST /users/get</c>.
/// </summary>
/// <remarks>
/// A read of one record is a POST, not a GET: a GET is a plain paged list and nothing else. The user is
/// read from the users domain table with one query for one row, which is everything a user response
/// shows, so no account is composed to answer it. A user that is not there answers 404; a store that
/// could not be read answers with its own status, because "not found" and "could not look" are
/// different facts.
/// </remarks>
public abstract class GetUserEndpointBase : Endpoint<UserScopedRequest, UserResponse>
{
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="GetUserEndpointBase"/> class.</summary>
    protected GetUserEndpointBase(ILogger logger, UserConfigurationProvider userProvider)
    {
        EndpointLogger = logger;
        _userProvider = userProvider;
    }

    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:read".
    /// </summary>
    protected virtual string ReadPolicy => "users:read";

    /// <inheritdoc />
    public override void Configure()
    {
        // API-66: the body accepts a Guid id OR a username via IdOrName.
        Post("/users/get");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(UserScopedRequest req, CancellationToken ct)
    {
        // By id when it parses as one, by name otherwise: the two are the domain record's own properties.
        var byId = Guid.TryParse(req.IdOrName, out var userId);
        var page = await _userProvider.Get(
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = byId ? nameof(DomainConfiguration.Id) : nameof(DomainConfiguration.Name),
                        Operator = FilterOperators.ByName("Equal"),
                        Value = byId ? (object)userId : req.IdOrName,
                    },
                },
                null, 0, 1, false),
            ct).ConfigureAwait(false);

        if (page.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(page, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await foreach (var user in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
        {
            await Send.OkAsync(MapToResponse(user), ct).ConfigureAwait(false);
            return;
        }

        if (page.Value.Outcome.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(page.Value.Outcome, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        // API-62: structured 404 envelope.
        await Send.ResultAsync(TypedResults.Json(new
        {
            errorCode = "NotFound",
            messages = new[] { $"User '{req.IdOrName}' was not found." }
        }, statusCode: 404)).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a user, as the users domain table holds it, to a response DTO. Override for custom mapping.
    /// </summary>
    protected abstract UserResponse MapToResponse(UserDomainConfiguration user);
}
