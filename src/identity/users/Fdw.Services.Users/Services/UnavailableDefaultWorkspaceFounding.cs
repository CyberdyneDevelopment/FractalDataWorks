using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Users.Services;

/// <summary>
/// The default-workspace founding on a host that does not run the workspaces service (FDW-4178). A tenant or an organization is founded
/// through <see cref="IDefaultWorkspaceFounding"/>, and only the workspaces domain implements it; a host that maps the sign-in and tenant
/// endpoints without the workspace endpoint group would otherwise fail at start with no service registered. This answers a failure when a
/// founding actually asks for a workspace, and the workspace group's real registration replaces it where that group is mapped.
/// </summary>
public sealed class UnavailableDefaultWorkspaceFounding : IDefaultWorkspaceFounding
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UnavailableDefaultWorkspaceFounding"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public UnavailableDefaultWorkspaceFounding(ILogger<UnavailableDefaultWorkspaceFounding>? logger = null)
    {
        _logger = logger ?? NullLogger<UnavailableDefaultWorkspaceFounding>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<Guid>> Found(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult<Guid>>(
            GenericResult<Guid>.Failure(DefaultWorkspaceFoundingLog.Unavailable(_logger, "made", workspace.WorkspaceId, workspace.TenantId)));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Remove(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(
            GenericResult.Failure(DefaultWorkspaceFoundingLog.Unavailable(_logger, "removed", workspace.WorkspaceId, workspace.TenantId)));
    }
}
