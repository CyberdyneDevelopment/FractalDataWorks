using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Logging;

/// <summary>
/// MessageLogging for graph pipelines (FDW-1396): the refusals made when a stored graph is turned into a flow and the run of it.
/// Every refusal is logged and returned in the failure result.
/// EventId range: 20950-20969.
/// </summary>
[MessageLoggingTypeCode("FLOW")]
public static partial class GraphLog
{
    /// <summary>Logs a graph with no components.</summary>
    [MessageLogging(EventId = 20950, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' has no components, so there is nothing to run")]
    public static partial IGenericMessage GraphEmpty(ILogger logger, string pipeline);

    /// <summary>Logs a run that was started without a value for a parameter a component or step setting names.</summary>
    [MessageLogging(EventId = 20968, Level = LogLevel.Error,
        Message = "'{owner}' names the parameter '{parameter}' in its {setting} setting, which this run was started without a value for")]
    public static partial IGenericMessage ParameterNotGiven(ILogger logger, string owner, string setting, string parameter);

    /// <summary>Logs a run whose value for a parameter cannot be the value of the setting that takes it.</summary>
    [MessageLogging(EventId = 20969, Level = LogLevel.Error,
        Message = "'{owner}' takes its {setting} setting from the parameter '{parameter}', whose value cannot be {kind}")]
    public static partial IGenericMessage ParameterNotConvertible(ILogger logger, string owner, string setting, string parameter, string kind);

    /// <summary>Logs a component of a kind that nothing builds a flow component for.</summary>
    [MessageLogging(EventId = 20953, Level = LogLevel.Error,
        Message = "Component '{component}' is of kind '{kind}', which a graph pipeline cannot run yet")]
    public static partial IGenericMessage ComponentKindNotRunnable(ILogger logger, string component, string kind);

    /// <summary>Logs a component whose settings the flow component refused.</summary>
    [MessageLogging(EventId = 20956, Level = LogLevel.Error,
        Message = "Component '{component}' of kind '{kind}' cannot be built: {reason}")]
    public static partial IGenericMessage ComponentInvalid(ILogger logger, string component, string kind, string? reason);

    /// <summary>Logs two components of one graph with the same name.</summary>
    [MessageLogging(EventId = 20957, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' has more than one component named '{component}'")]
    public static partial IGenericMessage ComponentNameRepeated(ILogger logger, string pipeline, string component);

    /// <summary>Logs an action or loop of a kind that nothing builds an engine step for.</summary>
    [MessageLogging(EventId = 20952, Level = LogLevel.Error,
        Message = "Component '{step}' is of kind '{kind}', which a graph pipeline cannot run as an action")]
    public static partial IGenericMessage StepKindNotRunnable(ILogger logger, string step, string kind);

    /// <summary>Logs an action or loop whose settings the engine step refused.</summary>
    [MessageLogging(EventId = 20954, Level = LogLevel.Error,
        Message = "Component '{step}' of kind '{kind}' cannot be built: {reason}")]
    public static partial IGenericMessage StepInvalid(ILogger logger, string step, string kind, string? reason);

    /// <summary>Logs a step setting whose stated type cannot read its text.</summary>
    [MessageLogging(EventId = 20955, Level = LogLevel.Error,
        Message = "Step '{step}' gives the setting '{setting}' the value '{value}', which the type '{type}' cannot read")]
    public static partial IGenericMessage StepSettingUnreadable(ILogger logger, string step, string setting, string? value, string type);

    /// <summary>Logs a variable a graph declares that cannot be declared.</summary>
    [MessageLogging(EventId = 20966, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' cannot declare the variable '{variable}': {reason}")]
    public static partial IGenericMessage VariableInvalid(ILogger logger, string pipeline, string variable, string reason);

    /// <summary>Logs a field mapping that is neither a source field nor a typed constant.</summary>
    [MessageLogging(EventId = 20958, Level = LogLevel.Error,
        Message = "Component '{component}' maps the field '{target}' from neither a source field nor a constant of a known type")]
    public static partial IGenericMessage FieldMappingIncomplete(ILogger logger, string component, string target);

    /// <summary>Logs a constant that its stated type cannot read.</summary>
    [MessageLogging(EventId = 20959, Level = LogLevel.Error,
        Message = "Component '{component}' gives the field '{target}' the constant '{value}', which the type '{type}' cannot read")]
    public static partial IGenericMessage ConstantUnreadable(ILogger logger, string component, string target, string? value, string type);

    /// <summary>Logs a join, lookup or key list whose setting names a collection option that does not exist.</summary>
    [MessageLogging(EventId = 20960, Level = LogLevel.Error,
        Message = "Component '{component}' names '{value}' for the setting '{setting}', which is not an option of {collection}")]
    public static partial IGenericMessage ChoiceUnknown(ILogger logger, string component, string setting, string value, string collection);

    /// <summary>Logs a graph run that had nothing to read through.</summary>
    [MessageLogging(EventId = 20962, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' cannot run: {missing} is required")]
    public static partial IGenericMessage RunDependencyMissing(ILogger logger, string pipeline, string missing);

    /// <summary>Logs a graph run that finished.</summary>
    [MessageLogging(EventId = 20963, Level = LogLevel.Information,
        Message = "Graph pipeline '{pipeline}' ran {components} components: {extracted} rows read, {loaded} rows loaded")]
    public static partial IGenericMessage RunFinished(ILogger logger, string pipeline, int components, long extracted, long loaded);

    /// <summary>Logs a parameter of a graph that cannot be declared for a run.</summary>
    [MessageLogging(EventId = 20964, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' cannot declare the parameter '{parameter}': {reason}")]
    public static partial IGenericMessage ParameterInvalid(ILogger logger, string pipeline, string parameter, string reason);

    /// <summary>Logs a run of a shape that only stands in for a component while a graph is checked.</summary>
    [MessageLogging(EventId = 20965, Level = LogLevel.Error,
        Message = "Component '{component}' is a shape that stands in for it while the graph is checked, and cannot run")]
    public static partial IGenericMessage ShapeCannotRun(ILogger logger, string component);

    /// <summary>Logs a record of how a step went that could not be written to the run's execution (FDW-1602).</summary>
    [MessageLogging(EventId = 20967, Level = LogLevel.Warning,
        Message = "The record of '{subject}' could not be written to the execution of the run: {reason}")]
    public static partial IGenericMessage ReportNotWritten(ILogger logger, string subject, string? reason);

    /// <summary>Logs an input mapping whose type, default or chain cannot be applied.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "Component '{component}' maps the field '{target}', but {reason}")]
    public static partial IGenericMessage FieldConversionInvalid(ILogger logger, string component, string target, string reason);
}
