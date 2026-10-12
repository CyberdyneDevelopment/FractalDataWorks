using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints.Logging;

/// <summary>
/// MessageLogging for the Compute stage a pipeline request states (FDW-1209): what is refused about its Built transformation
/// and its input mapping before the pipeline is saved. Each message is both logged and handed back as the reason of the problem
/// the caller reads.
/// EventId range: 20701-20799.
/// </summary>
[MessageLoggingTypeCode("COMPUTESTAGE")]
public static partial class ComputeStageLog
{
    /// <summary>Logs a Compute stage that names its Built transformation and also gives its id.</summary>
    [MessageLogging(EventId = 20701, Level = LogLevel.Warning,
        Message = "A Compute stage names its transformation and also gives its id; state one of them")]
    public static partial IGenericMessage NamesBoth(ILogger logger);

    /// <summary>Logs a Compute stage that names no transformation and gives no id.</summary>
    [MessageLogging(EventId = 20702, Level = LogLevel.Warning,
        Message = "A Compute stage names no transformation and gives no id; state one of them")]
    public static partial IGenericMessage NamesNeither(ILogger logger);

    /// <summary>Logs a named transformation that does not exist.</summary>
    [MessageLogging(EventId = 20703, Level = LogLevel.Warning,
        Message = "The Compute stage's transformation '{transformationName}' was not found")]
    public static partial IGenericMessage TransformationNotFound(ILogger logger, string transformationName);

    /// <summary>Logs a transformation id that does not exist.</summary>
    [MessageLogging(EventId = 20704, Level = LogLevel.Warning,
        Message = "The Compute stage's transformation {transformationId} was not found")]
    public static partial IGenericMessage TransformationIdNotFound(ILogger logger, Guid transformationId);

    /// <summary>Logs a transformation that is not a Built transformation.</summary>
    [MessageLogging(EventId = 20705, Level = LogLevel.Warning,
        Message = "The Compute stage's transformation '{transformationName}' is a {implementation}, which is not a Built transformation")]
    public static partial IGenericMessage TransformationNotBuilt(ILogger logger, string transformationName, string implementation);

    /// <summary>Logs a declared input that no mapping feeds.</summary>
    [MessageLogging(EventId = 20706, Level = LogLevel.Warning,
        Message = "Transformation '{transformationName}' declares input '{inputName}', but the Compute stage maps no field to it")]
    public static partial IGenericMessage InputNotMapped(ILogger logger, string transformationName, string inputName);

    /// <summary>Logs a mapping to an input the transformation does not declare.</summary>
    [MessageLogging(EventId = 20707, Level = LogLevel.Warning,
        Message = "The Compute stage maps a field to '{inputName}', which transformation '{transformationName}' does not declare as an input")]
    public static partial IGenericMessage InputNotDeclared(ILogger logger, string transformationName, string inputName);

    /// <summary>Logs an input that two mappings feed.</summary>
    [MessageLogging(EventId = 20708, Level = LogLevel.Warning,
        Message = "The Compute stage maps two fields to input '{inputName}'; an input has one field")]
    public static partial IGenericMessage InputMappedTwice(ILogger logger, string inputName);

    /// <summary>Logs a mapping with a blank input or field.</summary>
    [MessageLogging(EventId = 20709, Level = LogLevel.Warning,
        Message = "A Compute input mapping states a blank input name or a blank field")]
    public static partial IGenericMessage InputBlank(ILogger logger);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20710, Level = LogLevel.Warning,
        Message = "Transformation '{transformationName}' reads and writes tables, so the stage states no {member}; state table bindings, output routes and single values")]
    public static partial IGenericMessage RecordMemberInSetMode(ILogger logger, string transformationName, string member);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20711, Level = LogLevel.Warning,
        Message = "Transformation '{transformationName}' reads one record at a time, so the stage states no {member}; state the fields that feed its inputs")]
    public static partial IGenericMessage SetMemberInRecordMode(ILogger logger, string transformationName, string member);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20712, Level = LogLevel.Warning,
        Message = "Transformation '{transformationName}' declares table '{tableName}', but the Compute stage binds no rows to it")]
    public static partial IGenericMessage TableNotBound(ILogger logger, string transformationName, string tableName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20713, Level = LogLevel.Warning,
        Message = "The Compute stage binds rows to '{tableName}', which transformation '{transformationName}' does not declare as a table input")]
    public static partial IGenericMessage TableNotDeclared(ILogger logger, string transformationName, string tableName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20714, Level = LogLevel.Warning,
        Message = "The Compute stage binds rows to table '{tableName}' twice; a table has one source")]
    public static partial IGenericMessage TableBoundTwice(ILogger logger, string tableName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20715, Level = LogLevel.Warning,
        Message = "The source of table '{tableName}' is invalid: {reason}")]
    public static partial IGenericMessage TableSourceInvalid(ILogger logger, string tableName, string reason);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20716, Level = LogLevel.Warning,
        Message = "Transformation '{transformationName}' declares table output '{outputName}', but the Compute stage routes it nowhere")]
    public static partial IGenericMessage OutputNotRouted(ILogger logger, string transformationName, string outputName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20717, Level = LogLevel.Warning,
        Message = "The Compute stage routes '{outputName}', which transformation '{transformationName}' does not declare as a table output")]
    public static partial IGenericMessage OutputNotDeclared(ILogger logger, string transformationName, string outputName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20718, Level = LogLevel.Warning,
        Message = "The Compute stage routes table output '{outputName}' twice; an output goes to one place")]
    public static partial IGenericMessage OutputRoutedTwice(ILogger logger, string outputName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20719, Level = LogLevel.Warning,
        Message = "The route of table output '{outputName}' is invalid: {reason}")]
    public static partial IGenericMessage RouteInvalid(ILogger logger, string outputName, string reason);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20720, Level = LogLevel.Warning,
        Message = "The Compute stage routes {count} outputs to replace its rows; at most one can")]
    public static partial IGenericMessage ManyReplaceRoutes(ILogger logger, int count);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20721, Level = LogLevel.Warning,
        Message = "Transformation '{transformationName}' declares input '{inputName}', but the Compute stage gives it no value")]
    public static partial IGenericMessage ScalarNotGiven(ILogger logger, string transformationName, string inputName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20722, Level = LogLevel.Warning,
        Message = "The Compute stage gives a value to '{inputName}', which transformation '{transformationName}' does not declare as an input")]
    public static partial IGenericMessage ScalarNotDeclared(ILogger logger, string transformationName, string inputName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20723, Level = LogLevel.Warning,
        Message = "The Compute stage gives input '{inputName}' two values; an input has one")]
    public static partial IGenericMessage ScalarGivenTwice(ILogger logger, string inputName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20724, Level = LogLevel.Warning,
        Message = "Input '{inputName}' is given '{valueText}', which is not a {valueType}")]
    public static partial IGenericMessage ScalarInvalid(ILogger logger, string inputName, string valueText, string valueType);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20725, Level = LogLevel.Warning,
        Message = "Transformation '{transformationName}' has both value outputs and table outputs; a Compute stage applies one kind or the other")]
    public static partial IGenericMessage MixedOutputs(ILogger logger, string transformationName);

    /// <summary>Logs a refusal of a Compute stage that reads and writes tables.</summary>
    [MessageLogging(EventId = 20726, Level = LogLevel.Warning,
        Message = "The Compute stage reads and writes tables but does not say what a failing row does; state onRowFailure as Fail, Skip or Reject")]
    public static partial IGenericMessage RowFailureNotStated(ILogger logger);

    /// <summary>Logs a refusal of a Compute stage that states an action it does not have.</summary>
    [MessageLogging(EventId = 20727, Level = LogLevel.Warning,
        Message = "The Compute stage states onRowFailure '{action}', which is not Fail, Skip or Reject")]
    public static partial IGenericMessage RowFailureUnknown(ILogger logger, string action);
}
