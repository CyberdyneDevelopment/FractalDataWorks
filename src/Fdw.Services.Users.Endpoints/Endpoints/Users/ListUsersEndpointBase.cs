using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Models;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Pagination;
using Fdw.Web.RestEndpoints.ErrorMapping;
using System;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Base endpoint for listing users, one page at a time in name order (FDW-951).
/// </summary>
/// <remarks>
/// A page is one read of the users domain table: the fields a list shows live on the domain row, so no
/// user's account is read to answer it. A user store that cannot be read fails the request with its
/// own status rather than answering an empty page: "the store could not be read" and "there are no
/// users" are opposite facts, and an empty 200 would say the second when only the first is true.
/// </remarks>
public abstract class ListUsersEndpointBase : CrudPageEndpointBase<UserDomainConfiguration, UserResponse>
{
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>Initializes a new instance of the <see cref="ListUsersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userProvider">Reads the page of users.</param>
    protected ListUsersEndpointBase(ILogger logger, UserConfigurationProvider userProvider) : base(logger)
    {
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
    }

    /// <summary>Gets the user provider.</summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

    /// <summary>Gets the resource name used for the route and the default policy: <c>/users</c>, <c>users:read</c>.</summary>
    protected override string ResourceName => "users";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<UserDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
        => _userProvider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);

    /// <inheritdoc />
    protected override Task<IGenericResult<UserResponse>> ToSummary(UserDomainConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<UserResponse>.Success(MapToResponse(item)));

    /// <summary>Maps a user, as the users domain table holds it, to its response.</summary>
    /// <param name="user">The user's domain record.</param>
    /// <returns>The response.</returns>
    protected abstract UserResponse MapToResponse(UserDomainConfiguration user);
}
