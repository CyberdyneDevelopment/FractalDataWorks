using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for the refusing default of the default-workspace founding (FDW-4178). EventId range: 72739 (a tenant or organization
/// was asked to make or remove a default workspace on a host that has no workspaces service).
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class DefaultWorkspaceFoundingLog
{
    /// <summary>Logs that a default workspace was asked for on a host that does not run the workspaces service.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="operation">What was asked: making or removing.</param>
    /// <param name="workspaceId">The workspace.</param>
    /// <param name="tenantId">The tenant it belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 62739, Level = LogLevel.Error, Message = "The default workspace '{workspaceId}' of tenant '{tenantId}' could not be {operation}: this host does not run the workspaces service, so a tenant or an organization cannot be founded here")]
    public static partial IGenericMessage Unavailable(ILogger logger, string operation, Guid workspaceId, Guid tenantId);
}
