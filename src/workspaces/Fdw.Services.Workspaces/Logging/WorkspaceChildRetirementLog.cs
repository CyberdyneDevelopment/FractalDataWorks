using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for retiring a member, a relationship or a resource of a workspace by its Id (FDW-7024, FDW-7093).
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceChildRetirementLog
{
    /// <summary>Logs that one child row of a workspace was retired.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace.</param>
    /// <param name="container">The child container: WorkspaceMember, WorkspaceRelationship or WorkspaceResource.</param>
    /// <param name="childId">The retired row's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11764, Level = LogLevel.Information,
        Message = "Retired {container} row '{childId}' of workspace '{workspaceName}'")]
    public static partial IGenericMessage Retired(ILogger logger, string workspaceName, string container, string childId);

    /// <summary>Fails the retiring of a child row.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace.</param>
    /// <param name="container">The child container.</param>
    /// <param name="childId">The row's Id.</param>
    /// <param name="reason">Why it could not be retired.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91762, Level = LogLevel.Error,
        Message = "The {container} row '{childId}' of workspace '{workspaceName}' could not be retired: {reason}",
        ClientMessage = "It could not be removed from the workspace.")]
    public static partial IGenericMessage RetireFailed(ILogger logger, string workspaceName, string container, string childId, string reason);

    /// <summary>Fails a retire when the registration named no path for the workspace rows.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStore">The configuration connection the rows live on.</param>
    /// <param name="container">The child container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61761, Level = LogLevel.Critical,
        Message = "The {container} rows on '{dataStore}' cannot be reached: the workspace services were registered with no path for them",
        ClientMessage = "It could not be removed: the host's workspace services are not set up.")]
    public static partial IGenericMessage PathNotDeclared(ILogger logger, string dataStore, string container);
}
