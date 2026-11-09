using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Abstractions.Logging;

/// <summary>Messages of the decision whether founding makes a default workspace (FDW-4599).</summary>
[MessageLoggingTypeCode("WORKSPACEFOUND")]
public static partial class WorkspaceFoundingLog
{
    /// <summary>Logs the value the policy resolved and where it came from.</summary>
    [MessageLogging(EventId = 13301, Level = LogLevel.Debug, Message = "Founding makes a default workspace: {value} (from {source})")]
    public static partial IGenericMessage Resolved(ILogger logger, bool value, string source);

    /// <summary>Logs that the setting is stated by no settings row and declared by neither the option nor the host.</summary>
    [MessageLogging(EventId = 14301, Level = LogLevel.Error,
        Message = "Whether founding makes a default workspace is stated by no settings row and declared by neither the option nor the host, so nothing was founded",
        ClientMessage = "The setting 'FoundingMakesDefaultWorkspace' has no value and no declared default, so nothing was founded")]
    public static partial IGenericMessage SettingNotDeclared(ILogger logger);
}
