using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for making and removing an organization's default workspace (FDW-3668).
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class DefaultWorkspaceLog
{
    /// <summary>Logs that a default workspace is being made.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace's name.</param>
    /// <param name="organizationId">The organization it is the default of.</param>
    /// <param name="tenantId">The tenant it belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11700, Level = LogLevel.Information,
        Message = "Making default workspace '{workspaceName}' of organization {organizationId} in tenant {tenantId}")]
    public static partial IGenericMessage Founding(ILogger logger, string workspaceName, Guid organizationId, Guid tenantId);

    /// <summary>Logs that a default workspace was made.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace's name.</param>
    /// <param name="workspaceId">The workspace's id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11701, Level = LogLevel.Information,
        Message = "Made default workspace '{workspaceName}' ({workspaceId})")]
    public static partial IGenericMessage Founded(ILogger logger, string workspaceName, Guid workspaceId);

    /// <summary>Logs that a default workspace is being removed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceId">The workspace's id.</param>
    /// <param name="tenantId">The tenant it belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11702, Level = LogLevel.Information,
        Message = "Removing default workspace {workspaceId} of tenant {tenantId}")]
    public static partial IGenericMessage Removing(ILogger logger, Guid workspaceId, Guid tenantId);

    /// <summary>Logs that a new thing is being placed in an organization's default workspace (FDW-3692).</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The thing's name.</param>
    /// <param name="kind">What it is.</param>
    /// <param name="workspaceId">The default workspace it goes in.</param>
    /// <param name="organizationId">The organization whose default workspace that is.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11704, Level = LogLevel.Information,
        Message = "Placing {kind} '{name}' in default workspace {workspaceId} of organization {organizationId}")]
    public static partial IGenericMessage Placing(ILogger logger, string name, string kind, Guid workspaceId, Guid organizationId);

    /// <summary>Logs that the caller's authentication context was swapped for a run in the workspace's tenant.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tenantId">The tenant the run acts in.</param>
    /// <param name="callerTenantId">The tenant the caller was acting in, or empty when it had none.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11703, Level = LogLevel.Information,
        Message = "Acting in tenant {tenantId} to make its default workspace; the caller was acting in {callerTenantId}")]
    public static partial IGenericMessage ActingInOtherTenant(ILogger logger, Guid tenantId, string callerTenantId);
}
