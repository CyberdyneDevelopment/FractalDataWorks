using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// The whole service surface, cut to what the caller may call: <c>GET /openapi/reference</c>.
/// </summary>
/// <remarks>
/// The reference every host serves. An anonymous caller is shown only the operations that admit
/// anonymous callers; a signed-in caller every operation whose policies they hold; a platform admin
/// every operation. Anonymous, because the cut, not the route, is what keeps a caller to what they
/// may see.
/// </remarks>
public abstract class ServiceReferenceDocumentEndpointBase : ApiReferenceDocumentEndpointBase<EmptyRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ServiceReferenceDocumentEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="systemRoles">Decides whether the caller is a platform admin.</param>
    protected ServiceReferenceDocumentEndpointBase(
        ILogger<ApiReferenceDocumentEndpointBase<EmptyRequest>> logger,
        ISystemRoleConfiguration systemRoles)
        : base(logger, systemRoles)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/openapi/reference");
        AllowAnonymous();
        Description(builder => builder.ExcludeFromDescription());
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Filter(
        ApiReferenceDocument document,
        ApiReferenceCaller caller,
        EmptyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var admitted = document.KeepAdmitted(caller, Logger);
        return Task.FromResult<IGenericResult>(admitted);
    }
}
