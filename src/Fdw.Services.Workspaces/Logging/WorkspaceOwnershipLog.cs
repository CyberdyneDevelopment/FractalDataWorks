using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for <see cref="WorkspaceOwnershipReader"/>: which tenant owns a workspace, and which organization
/// names it as its default.
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceOwnershipLog
{
    /// <summary>Logs that a workspace was read with no owning tenant, which its table does not allow.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91061, Level = LogLevel.Error,
        Message = "Workspace '{workspaceName}' was read with no owning tenant; workspace.Workspace.TenantId is NOT NULL, so the read did not carry the column")]
    public static partial IGenericMessage NoOwningTenant(ILogger logger, string workspaceName);

    /// <summary>Logs who owns a workspace.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace.</param>
    /// <param name="tenantId">The tenant that owns it.</param>
    /// <param name="isDefault">Whether the tenant's default organization names it as its default.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11065, Level = LogLevel.Trace,
        Message = "Workspace '{workspaceName}' belongs to tenant {tenantId}; default for its organization: {isDefault}")]
    public static partial IGenericMessage OwnershipRead(ILogger logger, string workspaceName, string tenantId, bool isDefault);
}
