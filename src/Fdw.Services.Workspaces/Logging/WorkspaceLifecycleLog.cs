using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for creating and deleting a workspace together with its own permissions (FDW-937).
/// </summary>
/// <remarks>Declared here, beside the permissions service, so the endpoint packages log through it.</remarks>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceLifecycleLog
{
    /// <summary>Logs that a new workspace's permissions could not be made and the workspace is being undone.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace being undone.</param>
    /// <param name="reason">Why its permissions could not be made.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31020, Level = LogLevel.Warning,
        Message = "Undoing workspace '{workspaceName}': its permissions could not be created: {reason}")]
    public static partial IGenericMessage UndoingCreate(ILogger logger, string workspaceName, string reason);

    /// <summary>Logs that a half-made workspace was undone cleanly.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace that was undone.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information,
        Message = "Undid workspace '{workspaceName}' and any permissions it had been given")]
    public static partial IGenericMessage CreateUndone(ILogger logger, string workspaceName);
}
