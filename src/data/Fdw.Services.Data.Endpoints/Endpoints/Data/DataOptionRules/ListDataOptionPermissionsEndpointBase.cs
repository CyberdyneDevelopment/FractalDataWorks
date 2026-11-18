using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /data-option-permissions/read</c> (datastores:write), FDW-6339. Answers the permissions the caller holds: the ones the caller may set as the permission needed to change a rule.
/// Built by FDW-6489: the permissions of the signed-in caller, in name order; a caller who is not a signed-in person is refused with the service's code.
/// </summary>
public abstract class ListDataOptionPermissionsEndpointBase : DataOptionRuleServiceEndpointBase<EmptyRequest, List<string>>
{
    private readonly IAuthenticationContextAccessor _authContext;

    /// <summary>Initializes a new instance of the <see cref="ListDataOptionPermissionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="authContext">Who the caller is and the permissions they hold.</param>
    protected ListDataOptionPermissionsEndpointBase(ILogger? logger, IAuthenticationContextAccessor authContext)
        : base(logger)
    {
        _authContext = authContext;
    }

    /// <inheritdoc />
    protected override string RouteName => "ListDataOptionPermissions";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<string>>> Run(EmptyRequest request, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult<List<string>>>(_authContext.Current is { IsAuthenticated: true } context
            ? GenericResult<List<string>>.Success([.. context.Permissions.OrderBy(permission => permission, StringComparer.Ordinal)])
            : GenericResult<List<string>>.Failure(DataOptionRulesLog.CallerNotIdentified(EndpointLogger, "permissions")));

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data-option-permissions/read");
        Policies("datastores:write");
        Summary(s =>
        {
            s.Summary = "List the permissions the caller may require";
            s.Description = "Answers the permissions the caller holds: the ones the caller may set as the permission needed to change a rule.";
        });
        ConfigureEndpoint();
    }
}
