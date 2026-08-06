using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Roslyn.Commands.Abstractions.Logging;

/// <summary>
/// MessageLogging methods for Roslyn command operations.
/// EventId range: 9080-9083
/// </summary>
[MessageLoggingTypeCode("ROSLYN")]
public static partial class RoslynCommandLog
{
    /// <summary>
    /// Logs when a command type mismatch occurs.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Expected command of type {expectedType} but got {actualType}")]
    public static partial IGenericMessage CommandTypeMismatch(
        ILogger logger,
        string expectedType,
        string actualType);

    /// <summary>
    /// Logs when command execution fails.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Command execution failed: {error}")]
    public static partial IGenericMessage CommandExecutionFailed(
        ILogger logger,
        string? error);

    /// <summary>
    /// Logs when a command is executed successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Command {commandType} executed successfully")]
    public static partial IGenericMessage CommandExecuted(
        ILogger logger,
        string commandType);

    /// <summary>
    /// Logs when a translator is not found for a command.
    /// </summary>
    [MessageLogging(
        EventId = 60002,
        Level = LogLevel.Error,
        Message = "No translator found for command type {commandType}")]
    public static partial IGenericMessage TranslatorNotFound(
        ILogger logger,
        string commandType);

    /// <summary>
    /// Logs that a translator is starting work.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="translator">The concrete translator type.</param>
    /// <param name="commandType">The command it received.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// On the base class rather than in each translator, so all 92 report a consistent account of what
    /// ran without 92 copies of the same three lines — and, more importantly, without the coverage
    /// depending on whether anyone remembered. A translator can still add its own domain detail on top;
    /// what it cannot do is be silent.
    /// </remarks>
    [MessageLogging(
        EventId = 11065,
        Level = LogLevel.Trace,
        Message = "{translator} executing {commandType}")]
    public static partial IGenericMessage TranslatorExecuting(
        ILogger logger,
        string translator,
        string commandType);

    /// <summary>
    /// Logs what a translator produced.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="translator">The concrete translator type.</param>
    /// <param name="kind">Whether the result mutates the solution or only reads it.</param>
    /// <param name="changedFiles">Documents the result changed; zero for a query.</param>
    /// <param name="summary">The translator's own summary line.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// The changed-file count is the point. "Command completed successfully" is true of a refactor that
    /// touched two hundred files and of one that silently matched nothing, and telling those apart
    /// afterwards previously meant diffing the workspace.
    /// </remarks>
    [MessageLogging(
        EventId = 11066,
        Level = LogLevel.Debug,
        Message = "{translator} produced {kind} touching {changedFiles} document(s): {summary}")]
    public static partial IGenericMessage TranslatorProduced(
        ILogger logger,
        string translator,
        string kind,
        int changedFiles,
        string? summary);
}
