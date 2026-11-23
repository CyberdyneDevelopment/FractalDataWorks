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

    /// <summary>Logs a graph run refused because its options carry no execution id (FDW-4956): nothing it writes could be tied to an execution.</summary>
    [MessageLogging(EventId = 20970, Level = LogLevel.Error,
        Message = "Graph pipeline '{pipeline}' cannot run: its options carry no execution id, and a run is the execution that tracks it")]
    public static partial IGenericMessage RunHasNoExecutionId(ILogger logger, string pipeline);

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

    /// <summary>Logs a host that attached the graph pipeline but registered nothing that reads the pipelines that write each data set and their latest runs, which the data set status is judged by (FDW-4488, FDW-4588). The number is in the Configuration category (6).</summary>
    [MessageLogging(EventId = 60350, Level = LogLevel.Critical,
        Message = "The host runs graph pipelines but registered no IDataSetWriterRuns, which the status of a data set is judged by; register the graph pipeline service type's registration; the host does not start",
        ClientMessage = "The graph pipeline service type needs the data set writer runs to be registered, and they are not")]
    public static partial IGenericMessage DataSetWriterRunsNotRegistered(ILogger logger);

    /// <summary>
    /// Logs a graph pipeline type with no usable default for how often a destination writes what it has committed (FDW-4957): none declared, or
    /// below zero. The number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60351, Level = LogLevel.Critical,
        Message = "The {typeName} pipeline type declares no whole number of at least 0 for {field}; declare one in its DeclaredDefaults or state it with ServiceTypeDefaults.Override before Register; the host does not start")]
    public static partial IGenericMessage CommittedReportSecondsNotDeclared(ILogger logger, string typeName, string field);

    /// <summary>Logs the setting for how often a destination writes what it has committed stated below zero (FDW-4957); the run does not start.</summary>
    [MessageLogging(EventId = 60352, Level = LogLevel.Error,
        Message = "The setting {setting} is {seconds}; it must be at least 0 (0 writes every commit), or not set to take the pipeline type's default")]
    public static partial IGenericMessage CommittedReportSecondsNegative(ILogger logger, string setting, int seconds);

    /// <summary>
    /// Logs a graph pipeline type with no usable default for how many unmatched keys a report carries (FDW-4988): none declared, or below zero. The number
    /// is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60353, Level = LogLevel.Critical,
        Message = "The {typeName} pipeline type declares no whole number of at least 0 for {field}; declare one in its DeclaredDefaults or state it with ServiceTypeDefaults.Override before Register; the host does not start")]
    public static partial IGenericMessage UnmatchedKeySampleNotDeclared(ILogger logger, string typeName, string field);

    /// <summary>Logs the setting for how many unmatched keys a report carries stated below zero (FDW-4988); the run does not start.</summary>
    [MessageLogging(EventId = 60354, Level = LogLevel.Error,
        Message = "The setting {setting} is {size}; it must be at least 0 (0 carries only the count), or not set to take the pipeline type's default")]
    public static partial IGenericMessage UnmatchedKeySampleNegative(ILogger logger, string setting, int size);

    /// <summary>
    /// Logs a component whose component type's builder needs a build contribution this build was not given (FDW-6132, FDW-6240): the graph is refused before anything runs. The number
    /// is in the Configuration category (6): the host is missing a registration, not the graph a setting.
    /// </summary>
    [MessageLogging(EventId = 60355, Level = LogLevel.Error,
        Message = "Component '{component}' is of component type '{componentType}', whose builder needs the build contribution {contribution}, which this build was not given",
        ClientMessage = "A component of this pipeline needs a service the host has not registered")]
    public static partial IGenericMessage ComponentServiceMissing(ILogger logger, string component, string componentType, string contribution);

    /// <summary>
    /// Logs a component type whose builder needs a build contribution the host did not construct, found when the host starts (FDW-6132); one message for
    /// each component type and contribution. The number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60356, Level = LogLevel.Critical,
        Message = "The builder of component type '{componentType}' needs the build contribution {contribution}, which the host did not construct; the host does not start")]
    public static partial IGenericMessage ComponentServicesUnregistered(ILogger logger, string componentType, string contribution);

    /// <summary>
    /// Logs a component type with no builder of the collection it needs, found when the host starts (FDW-6133, FDW-6240): an action or loop type with no step
    /// builder of its name, or a type that moves rows with no component builder of its name. Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60357, Level = LogLevel.Critical,
        Message = "Component type '{componentType}' has no {builder} of its name, so no graph that uses it could be built; add one in the package of the type; the host does not start")]
    public static partial IGenericMessage ComponentTypeHasNoBuilder(ILogger logger, string componentType, string builder);

    /// <summary>
    /// Logs a component type that more than one builder names, found when the host starts (FDW-6240), so the build could not tell which makes its step.
    /// Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60364, Level = LogLevel.Critical,
        Message = "Component type '{componentType}' is named by {count} builders; a component type has exactly one, so remove the others; the host does not start")]
    public static partial IGenericMessage ComponentTypeHasTwoBuilders(ILogger logger, string componentType, int count);

    /// <summary>Logs a builder whose name is not a component type, found when the host starts (FDW-6240), so no component ever uses it. Configuration category (6).</summary>
    [MessageLogging(EventId = 60365, Level = LogLevel.Critical,
        Message = "The {builder} '{name}' names no component type; add the ComponentTypes option it builds, or remove it; the host does not start")]
    public static partial IGenericMessage BuilderNamesNoComponentType(ILogger logger, string name, string builder);

    /// <summary>
    /// Logs a component type with a declared setting that states no data type, found when the host starts (FDW-6133); one message for each component type and
    /// setting. Configuration category (6): the package of the component type declares the setting without saying what its value is.
    /// </summary>
    [MessageLogging(EventId = 60358, Level = LogLevel.Critical,
        Message = "Component type '{componentType}' declares the setting '{setting}', which no column of the component holds, and states no data type for it; state its DataType in the package of the component type; the host does not start")]
    public static partial IGenericMessage ComponentSettingDataTypeNotDeclared(ILogger logger, string componentType, string setting);

    /// <summary>Logs a stored declared setting whose text cannot be read as the data type its component type declares (FDW-6133). Validation category (2).</summary>
    [MessageLogging(EventId = 20370, Level = LogLevel.Error,
        Message = "Component '{component}' of component type '{componentType}' states '{text}' for its setting '{setting}', which cannot be read as {dataType}")]
    public static partial IGenericMessage ComponentSettingValueUnreadable(ILogger logger, string component, string componentType, string setting, string text, string dataType);

    /// <summary>Logs a declared setting whose text, rendered from the variables when the step runs, cannot be read as its data type (FDW-6133). Validation category (2).</summary>
    [MessageLogging(EventId = 20371, Level = LogLevel.Error,
        Message = "Component '{component}' reads its setting '{setting}' from '{text}' when the step runs, and the value it gives cannot be read as {dataType}")]
    public static partial IGenericMessage ComponentSettingRenderedUnreadable(ILogger logger, string component, string setting, string text, string dataType);

    /// <summary>Logs a declared setting whose text names a variable that is not declared, or a parameter the run left blank, when the step runs (FDW-6133). Missing category (3).</summary>
    [MessageLogging(EventId = 30370, Level = LogLevel.Error,
        Message = "Component '{component}' reads its setting '{setting}' from '{text}' when the step runs, and a variable it names has no value: {reason}")]
    public static partial IGenericMessage ComponentSettingVariableMissing(ILogger logger, Exception exception, string component, string setting, string text, string reason);

    /// <summary>Logs a builder that asks for a declared setting the stored component does not state (FDW-6133). Missing category (3).</summary>
    [MessageLogging(EventId = 30371, Level = LogLevel.Error,
        Message = "Component '{component}' of component type '{componentType}' does not state its setting '{setting}'")]
    public static partial IGenericMessage ComponentSettingNotStated(ILogger logger, string component, string componentType, string setting);

    /// <summary>Logs a builder that asks for a setting its component type does not take (FDW-6133). Configuration category (6): the package of the component type and its builder disagree.</summary>
    [MessageLogging(EventId = 60370, Level = LogLevel.Error,
        Message = "The builder of component '{component}' asks for the setting '{setting}', which its component type '{componentType}' does not take")]
    public static partial IGenericMessage ComponentSettingUndeclared(ILogger logger, string component, string componentType, string setting);

    /// <summary>
    /// Logs a builder that asks for a setting as a declared setting when the component holds it on a column, in child rows or in its stage (FDW-6133).
    /// Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60371, Level = LogLevel.Error,
        Message = "The builder of component '{component}' asks for the setting '{setting}' of component type '{componentType}' as a declared setting; the component holds it itself and the builder reads it from there")]
    public static partial IGenericMessage ComponentSettingHeldOnComponent(ILogger logger, string component, string componentType, string setting);

    /// <summary>Logs a builder that asks for a declared setting its component type states no data type for (FDW-6133). Configuration category (6).</summary>
    [MessageLogging(EventId = 60372, Level = LogLevel.Error,
        Message = "The builder of component '{component}' asks for the setting '{setting}', which its component type '{componentType}' declares with no data type")]
    public static partial IGenericMessage ComponentSettingDataTypeMissing(ILogger logger, string component, string componentType, string setting);

    /// <summary>Logs a build contribution the graph type registered because a collected builder needs it (FDW-6240). Logged-only category (1).</summary>
    [MessageLogging(EventId = 10357, Level = LogLevel.Trace,
        Message = "Registered the build contribution {contribution}, which the builder of '{builder}' needs")]
    public static partial IGenericMessage ComponentContributionRegistered(ILogger logger, string contribution, string builder);

    /// <summary>
    /// Logs a builder that names, among what it needs, a type that is not a concrete class implementing IFlowBuildContribution (FDW-6240); the host does not
    /// start. Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60359, Level = LogLevel.Critical,
        Message = "The builder of '{builder}' needs {type}, which is not a concrete class that implements IFlowBuildContribution, so it cannot be registered; the host does not start")]
    public static partial IGenericMessage ComponentNeedNotAContribution(ILogger logger, string builder, string type);

    /// <summary>
    /// Logs a build contribution registered more than once (FDW-6240): the graph type registers each one a builder needs, so a second is a hand registration
    /// beside it, and the build could not tell which to use; the host does not start. Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60361, Level = LogLevel.Critical,
        Message = "The build contribution {contribution} is registered {count} times; the graph type registers every contribution a builder needs, so remove the other registration; the host does not start")]
    public static partial IGenericMessage ComponentContributionRegisteredTwice(ILogger logger, string contribution, int count);

    /// <summary>Logs build contributions that cannot be constructed, most often because a dependency of one is not registered (FDW-6240); the host does not start. Configuration category (6).</summary>
    [MessageLogging(EventId = 60362, Level = LogLevel.Critical,
        Message = "A build contribution cannot be constructed: {reason}; register what its constructor takes in that service's own domain; the host does not start")]
    public static partial IGenericMessage ComponentContributionUnresolvable(ILogger logger, Exception exception, string reason);

    /// <summary>Logs a registered build contribution that no collected builder names (FDW-6240); the host starts. Configuration category (6).</summary>
    [MessageLogging(EventId = 60363, Level = LogLevel.Warning,
        Message = "The build contribution {contribution} is registered and no builder needs it; nothing reads it")]
    public static partial IGenericMessage ComponentContributionUnused(ILogger logger, string contribution);
}
