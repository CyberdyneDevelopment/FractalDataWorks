using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Etl.Projects.UI.Components.Logging;

/// <summary>
/// MessageLogging methods for ProjectProvider operations.
/// EventId range: 8780-8791
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS2")]
public static partial class ProjectProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Projects (8780-8781)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the projects list fails.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to load projects list")]
    public static partial IGenericMessage LoadProjectsFailed(
        ILogger logger);

    /// <summary>Logs when loading the projects list fails with exception.</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to load projects list")]
    public static partial IGenericMessage LoadProjectsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Project (8782-8783)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading a project detail fails.</summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to load project '{projectName}'")]
    public static partial IGenericMessage ProjectDetailLoadFailed(
        ILogger logger,
        string projectName);

    /// <summary>Logs when loading a project detail fails with exception.</summary>
    [MessageLogging(EventId = 71013, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to load project detail")]
    public static partial IGenericMessage ProjectDetailLoadException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Project (8784-8785)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a project fails.</summary>
    [MessageLogging(EventId = 71014, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to create project")]
    public static partial IGenericMessage ProjectCreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a project fails with exception.</summary>
    [MessageLogging(EventId = 71015, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to create project")]
    public static partial IGenericMessage ProjectCreateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Project (8786-8787)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a project fails.</summary>
    [MessageLogging(EventId = 71016, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to update project '{projectId}'")]
    public static partial IGenericMessage ProjectUpdateFailed(
        ILogger logger,
        string projectId);

    /// <summary>Logs when updating a project fails with exception.</summary>
    [MessageLogging(EventId = 71017, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to update project")]
    public static partial IGenericMessage ProjectUpdateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Project (8788-8789)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a project fails.</summary>
    [MessageLogging(EventId = 71018, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to delete project '{projectId}'")]
    public static partial IGenericMessage ProjectDeleteFailed(
        ILogger logger,
        string projectId);

    /// <summary>Logs when deleting a project fails with exception.</summary>
    [MessageLogging(EventId = 71019, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to delete project")]
    public static partial IGenericMessage ProjectDeleteException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trigger (8790-8791)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when triggering a project execution fails.</summary>
    [MessageLogging(EventId = 71020, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to trigger project '{projectName}'")]
    public static partial IGenericMessage ProjectTriggerFailed(
        ILogger logger,
        string projectName);

    /// <summary>Logs when triggering a project execution fails with exception.</summary>
    [MessageLogging(EventId = 71021, Level = LogLevel.Error,
        Message = "ProjectProvider: Failed to trigger project")]
    public static partial IGenericMessage ProjectTriggerException(
        ILogger logger,
        Exception exception);
}
