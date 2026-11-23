using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the typed outputs of a pipeline (FDW-6136). Each refusal is logged and returned in the failure result, numbered in the category of what it says: a graph, a mapping or a value that is
/// not valid is 2 (the save refuses it, or the run does when a value does not read as its stated type), an output the started run did not record is not found (3), and a row that cannot be read or written
/// is a dependency that failed (7). EventId range: 20888, 20889, 20896 to 20898, 20900, 30903, 70906 and 70907.
/// </summary>
[MessageLoggingTypeCode("PIPELINEOUTPUT")]
public static partial class PipelineOutputLog
{
    /// <summary>Logs a graph whose output names a variable the graph does not declare.</summary>
    [MessageLogging(EventId = 20896, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' declares the output '{output}' from the variable '{variable}', which the graph does not declare",
        ClientMessage = "An output has to be kept in a variable the pipeline declares")]
    public static partial IGenericMessage OutputVariableUnknown(ILogger logger, string pipeline, string output, string variable);

    /// <summary>Logs an output whose type is not the type of its variable, or is not a data type.</summary>
    [MessageLogging(EventId = 20897, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' declares the output '{output}' as {outputType} but its variable '{variable}' is {variableType}, or {outputType} is not a data type",
        ClientMessage = "An output has the type of the variable that holds it, and the type has to be a data type")]
    public static partial IGenericMessage OutputTypeMismatch(ILogger logger, string pipeline, string output, string outputType, string variable, string variableType);

    /// <summary>Logs a graph that declares one output name twice.</summary>
    [MessageLogging(EventId = 20898, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' declares the output '{output}' more than once",
        ClientMessage = "An output name is declared once")]
    public static partial IGenericMessage OutputNameDuplicate(ILogger logger, string pipeline, string output);

    /// <summary>Logs the value of an output that does not read as its stated type when the run ends.</summary>
    [MessageLogging(EventId = 20900, Level = LogLevel.Error,
        Message = "The output '{output}' of pipeline '{pipeline}' holds a value that is not a {dataType}, so the run cannot give it back",
        ClientMessage = "An output's value has to be of the type the pipeline states for it")]
    public static partial IGenericMessage OutputInvalid(ILogger logger, string pipeline, string output, string dataType);

    /// <summary>Logs an Execute pipeline step that maps outputs while it does not wait for the pipeline it starts.</summary>
    [MessageLogging(EventId = 20888, Level = LogLevel.Error,
        Message = "Execute pipeline '{step}' maps outputs but does not wait for the pipeline it starts, so there are none to read",
        ClientMessage = "A step reads the outputs of the pipeline it starts only when it waits for that pipeline")]
    public static partial IGenericMessage OutputsNeedAwait(ILogger logger, string step);

    /// <summary>Logs an outputs setting that is not lines of an output name, an equals sign and a variable name.</summary>
    [MessageLogging(EventId = 20889, Level = LogLevel.Error,
        Message = "Execute pipeline '{step}' states its outputs as '{text}', which is not lines of an output name, an equals sign and a variable name",
        ClientMessage = "Map each output on its own line, as output=variable")]
    public static partial IGenericMessage OutputMapMalformed(ILogger logger, string step, string text);

    /// <summary>Logs an output the started pipeline did not record.</summary>
    [MessageLogging(EventId = 30903, Level = LogLevel.Error,
        Message = "Execute pipeline '{step}' reads the output '{output}' of '{pipeline}', which that run did not record",
        ClientMessage = "The pipeline the step started gave back no output of that name")]
    public static partial IGenericMessage OutputMissing(ILogger logger, string step, string output, string pipeline);

    /// <summary>Logs the outputs of a run that could not be read.</summary>
    [MessageLogging(EventId = 70906, Level = LogLevel.Error,
        Message = "The outputs of run {executionId} could not be read: {reason}",
        ClientMessage = "The outputs of the run could not be read")]
    public static partial IGenericMessage NotRead(ILogger logger, Guid executionId, string? reason);

    /// <summary>Logs the outputs of a run that could not be written.</summary>
    [MessageLogging(EventId = 70907, Level = LogLevel.Error,
        Message = "The outputs of run {executionId} could not be recorded: {reason}",
        ClientMessage = "The outputs of the run could not be recorded")]
    public static partial IGenericMessage NotWritten(ILogger logger, Guid executionId, string? reason);
}
