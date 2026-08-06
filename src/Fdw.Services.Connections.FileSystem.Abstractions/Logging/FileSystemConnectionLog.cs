using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.FileSystem.Abstractions.Logging;

/// <summary>
/// MessageLogging for the FileSystem connection.
/// EventId range: 9550-9574
/// </summary>
[MessageLoggingTypeCode("FS")]
public static partial class FileSystemConnectionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (9550-9554)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the FileSystem connection is reading text from the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path being read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' reading text from '{relativePath}'")]
    public static partial IGenericMessage ReadingText(ILogger logger, string connectionName, string relativePath);

    /// <summary>
    /// Logs that the FileSystem connection is reading bytes from the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path being read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' reading bytes from '{relativePath}'")]
    public static partial IGenericMessage ReadingBytes(ILogger logger, string connectionName, string relativePath);

    /// <summary>
    /// Logs that the FileSystem connection is checking for the existence of the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path whose existence is being checked.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' checking existence of '{relativePath}'")]
    public static partial IGenericMessage CheckingExists(ILogger logger, string connectionName, string relativePath);

    /// <summary>
    /// Logs that the FileSystem connection is writing text to the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path being written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' writing text to '{relativePath}'")]
    public static partial IGenericMessage WritingText(ILogger logger, string connectionName, string relativePath);

    /// <summary>
    /// Logs that the FileSystem connection is writing bytes to the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path being written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' writing bytes to '{relativePath}'")]
    public static partial IGenericMessage WritingBytes(ILogger logger, string connectionName, string relativePath);

    // ═══════════════════════════════════════════════════════════════════════════
    // Information (9560-9564)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the FileSystem connection was created with the given root directory.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="root">The root directory the connection was created with.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' created with root '{root}'")]
    public static partial IGenericMessage Created(ILogger logger, string connectionName, string root);

    /// <summary>
    /// Logs that the FileSystem connection finished reading text from the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path that was read.</param>
    /// <param name="charCount">The number of characters that were read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' read text from '{relativePath}' ({charCount} chars)")]
    public static partial IGenericMessage ReadTextCompleted(ILogger logger, string connectionName, string relativePath, int charCount);

    /// <summary>
    /// Logs that the FileSystem connection finished reading bytes from the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path that was read.</param>
    /// <param name="byteCount">The number of bytes that were read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' read {byteCount} bytes from '{relativePath}'")]
    public static partial IGenericMessage ReadBytesCompleted(ILogger logger, string connectionName, string relativePath, int byteCount);

    /// <summary>
    /// Logs that the FileSystem connection finished writing text to the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path that was written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' wrote text to '{relativePath}'")]
    public static partial IGenericMessage WriteTextCompleted(ILogger logger, string connectionName, string relativePath);

    /// <summary>
    /// Logs that the FileSystem connection finished writing bytes to the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path that was written.</param>
    /// <param name="byteCount">The number of bytes that were written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' wrote {byteCount} bytes to '{relativePath}'")]
    public static partial IGenericMessage WriteBytesCompleted(ILogger logger, string connectionName, string relativePath, int byteCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warning (9570-9572)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the FileSystem connection could not find a file at the resolved path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="resolvedPath">The fully resolved path where the file was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 30000, Level = LogLevel.Warning, Message = "FileSystem connection '{connectionName}': file not found at '{resolvedPath}'")]
    public static partial IGenericMessage FileNotFound(ILogger logger, string connectionName, string resolvedPath);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (9571-9574)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the FileSystem connection denied a request because the relative path resolves outside the configured root.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path that was denied for path traversal.</param>
    /// <param name="root">The configured root directory the path resolved outside of.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 50001, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': path traversal denied for '{relativePath}' (resolves outside Root '{root}')")]
    public static partial IGenericMessage PathTraversalDenied(ILogger logger, string connectionName, string relativePath, string root);

    /// <summary>
    /// Logs that the FileSystem connection's configured root directory does not exist.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="root">The configured root directory that does not exist.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': Root directory does not exist: '{root}'")]
    public static partial IGenericMessage RootDoesNotExist(ILogger logger, string connectionName, string root);

    /// <summary>
    /// Logs that an I/O failure occurred on the FileSystem connection for the given relative path.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised during the I/O operation.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="relativePath">The relative path on which the I/O failure occurred.</param>
    /// <param name="message">The failure message describing the I/O error.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': I/O failure on '{relativePath}': {message}")]
    public static partial IGenericMessage IoFailed(ILogger logger, Exception exception, string connectionName, string relativePath, string message);

    /// <summary>
    /// Logs that factory validation failed for the FileSystem connection.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="reason">The reason factory validation failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': factory validation failed — {reason}")]
    public static partial IGenericMessage FactoryValidationFailed(ILogger logger, string connectionName, string reason);

    /// <summary>
    /// Logs that cleanup of an orphaned temp file (left behind by a failed write-then-atomic-rename) itself
    /// failed. Best-effort only — the original write/move failure has already been produced as the
    /// operation's result; this does not change that result.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception raised while deleting the orphaned temp file.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="tempPath">The orphaned temp file path that could not be cleaned up.</param>
    /// <param name="message">The failure message describing the cleanup error.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 81000, Level = LogLevel.Warning, Message = "FileSystem connection '{connectionName}': failed to clean up orphaned temp file '{tempPath}': {message}")]
    public static partial IGenericMessage TempFileCleanupFailed(ILogger logger, Exception exception, string connectionName, string tempPath, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Health Probe Events (ISupportsHealthProbe)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the FileSystem connection is starting a health probe of its configured root.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="root">The configured root directory being probed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' probing root '{root}'")]
    public static partial IGenericMessage ProbeStarting(ILogger logger, string connectionName, string root);

    /// <summary>
    /// Logs that the FileSystem connection's health probe succeeded (the configured root exists).
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="root">The configured root directory that was verified to exist.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}': health probe succeeded, root '{root}' exists")]
    public static partial IGenericMessage ProbeSucceeded(ILogger logger, string connectionName, string root);
}
