using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Commands.Abstractions.Logging;

/// <summary>
/// Static logger class for command operations.
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS")]
public static partial class CommandLogger
{
    /// <summary>
    /// Logs the start of command execution.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type of command being executed.</param>
    /// <param name="commandId">The unique identifier of the command.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Executing command {commandType} with ID {commandId}")]
    public static partial IGenericMessage CommandExecutionStarted(
        ILogger logger,
        string commandType,
        Guid commandId);

    /// <summary>
    /// Logs successful command completion.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type of command that completed.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Command {commandType} completed successfully in {elapsedMs}ms")]
    public static partial IGenericMessage CommandExecutionCompleted(
        ILogger logger,
        string commandType,
        long elapsedMs);

    /// <summary>
    /// Logs command execution failure.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="commandType">The type of command that failed.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Command {commandType} failed: {errorMessage}")]
    public static partial IGenericMessage CommandExecutionFailed(
        ILogger logger,
        Exception exception,
        string commandType,
        string errorMessage);

    /// <summary>
    /// Logs command validation started.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type of command being validated.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Validating command {commandType}")]
    public static partial IGenericMessage CommandValidationStarted(
        ILogger logger,
        string commandType);

    /// <summary>
    /// Logs command validation failure.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type of command that failed validation.</param>
    /// <param name="validationError">The validation error message.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Command validation failed for {commandType}: {validationError}")]
    public static partial IGenericMessage CommandValidationFailed(
        ILogger logger,
        string commandType,
        string validationError);

    /// <summary>
    /// Logs the start of command translation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="sourceFormat">The source format of the command.</param>
    /// <param name="targetFormat">The target format for translation.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Translating command from {sourceFormat} to {targetFormat}")]
    public static partial IGenericMessage TranslationStarted(
        ILogger logger,
        string sourceFormat,
        string targetFormat);

    /// <summary>
    /// Logs successful command translation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="sourceFormat">The source format of the command.</param>
    /// <param name="targetFormat">The target format for translation.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Translation completed from {sourceFormat} to {targetFormat} in {elapsedMs}ms")]
    public static partial IGenericMessage TranslationCompleted(
        ILogger logger,
        string sourceFormat,
        string targetFormat,
        long elapsedMs);

    /// <summary>
    /// Logs command translation failure.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="sourceFormat">The source format of the command.</param>
    /// <param name="targetFormat">The target format for translation.</param>
    /// <param name="reason">The reason for translation failure.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Warning,
        Message = "Translation failed from {sourceFormat} to {targetFormat}: {reason}")]
    public static partial IGenericMessage TranslationFailed(
        ILogger logger,
        string sourceFormat,
        string targetFormat,
        string reason);

    /// <summary>
    /// Logs translator selection.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="translatorType">The type of translator selected.</param>
    /// <param name="sourceFormat">The source format of the command.</param>
    /// <param name="targetFormat">The target format for translation.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Selected translator {translatorType} for {sourceFormat} to {targetFormat}")]
    public static partial IGenericMessage TranslatorSelected(
        ILogger logger,
        string translatorType,
        string sourceFormat,
        string targetFormat);

    /// <summary>
    /// Logs when no translator is found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="sourceFormat">The source format of the command.</param>
    /// <param name="targetFormat">The target format for translation.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "No translator found for {sourceFormat} to {targetFormat}")]
    public static partial IGenericMessage TranslatorNotFound(
        ILogger logger,
        string sourceFormat,
        string targetFormat);

    /// <summary>
    /// Logs bulk command processing started.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="itemCount">The total number of items to process.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Starting bulk command processing with {itemCount} items in batches of {batchSize}")]
    public static partial IGenericMessage BulkProcessingStarted(
        ILogger logger,
        int itemCount,
        int batchSize);

    /// <summary>
    /// Logs bulk batch completion.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="batchNumber">The current batch number.</param>
    /// <param name="totalBatches">The total number of batches.</param>
    /// <param name="processedCount">The number of items processed so far.</param>
    /// <param name="totalCount">The total number of items.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Completed batch {batchNumber} of {totalBatches} ({processedCount}/{totalCount} items)")]
    public static partial IGenericMessage BulkBatchCompleted(
        ILogger logger,
        int batchNumber,
        int totalBatches,
        int processedCount,
        int totalCount);

    /// <summary>
    /// Logs bulk command processing completion.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="successCount">The number of successful operations.</param>
    /// <param name="failureCount">The number of failed operations.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Bulk command processing completed: {successCount} succeeded, {failureCount} failed in {elapsedMs}ms")]
    public static partial IGenericMessage BulkProcessingCompleted(
        ILogger logger,
        int successCount,
        int failureCount,
        long elapsedMs);

    /// <summary>
    /// Logs command caching.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type of command being cached.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="durationSeconds">The cache duration in seconds.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Caching command result for {commandType} with key {cacheKey} for {durationSeconds}s")]
    public static partial IGenericMessage CommandCached(
        ILogger logger,
        string commandType,
        string cacheKey,
        int durationSeconds);

    /// <summary>
    /// Logs cache hit.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type of command with cache hit.</param>
    /// <param name="cacheKey">The cache key that was hit.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Cache hit for command {commandType} with key {cacheKey}")]
    public static partial IGenericMessage CacheHit(
        ILogger logger,
        string commandType,
        string cacheKey);

    /// <summary>
    /// Logs cache miss.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type of command with cache miss.</param>
    /// <param name="cacheKey">The cache key that was missed.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Debug,
        Message = "Cache miss for command {commandType} with key {cacheKey}")]
    public static partial IGenericMessage CacheMiss(
        ILogger logger,
        string commandType,
        string cacheKey);
}
