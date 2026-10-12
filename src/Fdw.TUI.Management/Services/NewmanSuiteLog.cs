using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// MessageLogging for the Newman suite service.
/// EventId range: 9700-9719.
/// </summary>
/// <remarks>
/// Every one of these doubles as the failure a caller receives: the service returns results
/// rather than throwing, and the message in the result is the message that was logged. A
/// screen can therefore show the operator exactly what the log says, rather than a
/// paraphrase of it.
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class NewmanSuiteLog
{
    /// <summary>Logs the suite directory the service resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="directory">The resolved directory.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9700,
        Level = LogLevel.Trace,
        Message = "Newman suite directory resolved to {directory}")]
    public static partial IGenericMessage SuiteDirectoryResolved(ILogger logger, string directory);

    /// <summary>Logs a suite directory that does not exist.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="directory">The directory that was looked for.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9701,
        Level = LogLevel.Error,
        Message = "No Newman suite at {directory}. Set FDW_NEWMAN_DIR to the newman folder, or run the suite's own scripts once to create it")]
    public static partial IGenericMessage SuiteDirectoryMissing(ILogger logger, string directory);

    /// <summary>Logs a collection file that has not been generated yet.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">Where the collection was expected.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9702,
        Level = LogLevel.Error,
        Message = "No generated collection at {path}. Refresh the suite to pull the OpenAPI document and generate it")]
    public static partial IGenericMessage CollectionMissing(ILogger logger, string path);

    /// <summary>Logs a collection that could not be read as a collection.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The file that was read.</param>
    /// <param name="reason">What went wrong.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9703,
        Level = LogLevel.Error,
        Message = "The collection at {path} could not be read: {reason}")]
    public static partial IGenericMessage CollectionUnreadable(ILogger logger, string path, string reason);

    /// <summary>Logs the folders read out of the collection.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="folderCount">How many folders.</param>
    /// <param name="requestCount">How many requests across them.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9704,
        Level = LogLevel.Debug,
        Message = "Collection holds {folderCount} folder(s) and {requestCount} request(s)")]
    public static partial IGenericMessage CollectionRead(ILogger logger, int folderCount, int requestCount);

    /// <summary>Logs a suite run starting.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scope">The folder being run, or the whole suite.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9705,
        Level = LogLevel.Information,
        Message = "Running the Newman suite — scope {scope}")]
    public static partial IGenericMessage RunStarting(ILogger logger, string scope);

    /// <summary>Logs a completed run.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="requests">Requests sent.</param>
    /// <param name="assertions">Assertions evaluated.</param>
    /// <param name="failures">Assertions failed.</param>
    /// <param name="durationMs">How long it took.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9706,
        Level = LogLevel.Information,
        Message = "Run finished — {requests} request(s), {assertions} assertion(s), {failures} failure(s) in {durationMs}ms")]
    public static partial IGenericMessage RunFinished(ILogger logger, int requests, int assertions, int failures, long durationMs);

    /// <summary>Logs a run that could not be started.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why not.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9707,
        Level = LogLevel.Error,
        Message = "The suite could not be run: {reason}")]
    public static partial IGenericMessage RunFailed(ILogger logger, string reason);

    /// <summary>Logs a missing test credential.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9708,
        Level = LogLevel.Error,
        Message = "FDW_TEST_PASSWORD is not set. The suite signs in before it runs, and this is not guessed")]
    public static partial IGenericMessage TestPasswordMissing(ILogger logger);

    /// <summary>Logs a spec refresh starting.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9709,
        Level = LogLevel.Information,
        Message = "Pulling the OpenAPI document and regenerating the collection")]
    public static partial IGenericMessage RefreshStarting(ILogger logger);

    /// <summary>Logs a completed refresh.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="paths">Paths in the document.</param>
    /// <param name="operations">Operations across them.</param>
    /// <param name="requests">Requests generated.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9710,
        Level = LogLevel.Information,
        Message = "Refreshed — {paths} path(s), {operations} operation(s), {requests} generated request(s)")]
    public static partial IGenericMessage RefreshFinished(ILogger logger, int paths, int operations, int requests);

    /// <summary>Logs a refresh that failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9711,
        Level = LogLevel.Error,
        Message = "The suite could not be refreshed: {reason}")]
    public static partial IGenericMessage RefreshFailed(ILogger logger, string reason);

    /// <summary>Logs an absent run record.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">Where the record was expected.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9712,
        Level = LogLevel.Warning,
        Message = "No run record at {path}. Run the suite before asking what failed")]
    public static partial IGenericMessage NoRunRecord(ILogger logger, string path);
}
