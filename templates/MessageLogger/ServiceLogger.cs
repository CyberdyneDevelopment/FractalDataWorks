using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.YourNamespace;

/// <summary>
/// Static logger class for ServiceLogger messages.
/// Uses source generation to create high-performance logging methods that return IGenericMessage.
/// </summary>
public static partial class ServiceLogger
{
#if includeExamples
    // Example: Error message with parameters
    [MessageLogging(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Connection to {serverName} on port {port} failed")]
    public static partial IGenericMessage ConnectionFailed(ILogger logger, string serverName, int port);

    // Example: Information message with single parameter
    [MessageLogging(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Service started successfully on {hostName}")]
    public static partial IGenericMessage ServiceStarted(ILogger logger, string hostName);

    // Example: Warning message without parameters
    [MessageLogging(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Service is running in degraded mode")]
    public static partial IGenericMessage DegradedMode(ILogger logger);

    // Example: Critical error with exception
    [MessageLogging(
        EventId = 1004,
        Level = LogLevel.Critical,
        Message = "Unexpected error occurred while processing {operation}")]
    public static partial IGenericMessage UnexpectedError(ILogger logger, Exception exception, string operation);

    // Example: Debug message with custom severity
    [MessageLogging(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "Processing item {itemId} with {itemCount} total items",
        Severity = MessageSeverity.Debug,
        AutoMapSeverity = false)]
    public static partial IGenericMessage ProcessingItem(ILogger logger, int itemId, int itemCount);

    // Example: Trace message for detailed diagnostics
    [MessageLogging(
        EventId = 1006,
        Level = LogLevel.Trace,
        Message = "Method {methodName} executed in {elapsedMs}ms")]
    public static partial IGenericMessage MethodExecuted(ILogger logger, string methodName, long elapsedMs);
#endif

    // TODO: Add your logging methods here
    // Follow the pattern above:
    // 1. Use [MessageLogging] attribute with EventId, Level, and Message
    // 2. Make the method public static partial
    // 3. Return IGenericMessage
    // 4. First parameter must be ILogger
    // 5. Additional parameters become message template arguments
}
