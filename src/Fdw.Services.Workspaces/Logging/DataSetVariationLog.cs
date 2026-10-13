using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for creating and deleting a data set variation inside a workspace (FDW-1022, FDW-1318).
/// </summary>
/// <remarks>Declared here, beside the other workspace lifecycle log, so the endpoint packages log through it.</remarks>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class DataSetVariationLog
{
    /// <summary>Logs that a variation is about to be created.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The variation's name.</param>
    /// <param name="workspaceName">The workspace that will own it.</param>
    /// <param name="baseName">The main data set it varies.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11243, Level = LogLevel.Information,
        Message = "Creating data set '{dataSetName}' in workspace '{workspaceName}' as a variation of '{baseName}'")]
    public static partial IGenericMessage CreatingVariation(ILogger logger, string dataSetName, string workspaceName, string baseName);

    /// <summary>Logs that a variation was created.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The variation's name.</param>
    /// <param name="workspaceName">The workspace that owns it.</param>
    /// <param name="baseName">The main data set it varies.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11244, Level = LogLevel.Information,
        Message = "Created variation '{dataSetName}' of '{baseName}' in workspace '{workspaceName}'")]
    public static partial IGenericMessage CreatedVariation(ILogger logger, string dataSetName, string workspaceName, string baseName);

    /// <summary>Logs that a variation is about to be deleted.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The variation's name.</param>
    /// <param name="workspaceName">The workspace that owns it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11245, Level = LogLevel.Information,
        Message = "Deleting variation '{dataSetName}' of workspace '{workspaceName}'")]
    public static partial IGenericMessage DeletingVariation(ILogger logger, string dataSetName, string workspaceName);

    /// <summary>Logs that a variation was deleted.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The variation's name.</param>
    /// <param name="workspaceName">The workspace that owned it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11247, Level = LogLevel.Information,
        Message = "Deleted variation '{dataSetName}' of workspace '{workspaceName}'")]
    public static partial IGenericMessage DeletedVariation(ILogger logger, string dataSetName, string workspaceName);
}
