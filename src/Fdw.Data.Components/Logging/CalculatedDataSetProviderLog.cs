using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for CalculatedDataSetProvider operations.
/// EventId ranges: 4180-4189 (pipeline ops), 4061-4066 (compile ops — free block within 4058-4099)
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class CalculatedDataSetProviderLog
{
    /// <summary>
    /// Logs that the pipeline designer is being loaded for the named dataset.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose pipeline designer is loading.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Loading pipeline designer for dataset '{name}'")]
    public static partial IGenericMessage LoadingDesigner(ILogger logger, string name);

    /// <summary>
    /// Logs that a pipeline was found for the named dataset.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the pipeline that was found.</param>
    /// <param name="name">The name of the dataset the pipeline belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Found pipeline {id} for dataset '{name}'")]
    public static partial IGenericMessage FoundPipeline(ILogger logger, Guid id, string name);

    /// <summary>
    /// Logs that loading the pipelines for the named dataset failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose pipelines failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "Failed to load pipelines for dataset '{name}'")]
    public static partial IGenericMessage LoadPipelinesFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception was thrown while loading the pipeline designer for the named dataset.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the pipeline designer.</param>
    /// <param name="name">The name of the dataset whose pipeline designer failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Failed to load pipeline designer for dataset '{name}'")]
    public static partial IGenericMessage LoadDesignerException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that loading the pipeline detail for the named dataset failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose pipeline detail failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Warning,
        Message = "Failed to load pipeline detail for dataset '{name}'")]
    public static partial IGenericMessage LoadPipelineDetailFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that the pipeline for the named dataset is being saved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose pipeline is being saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Saving pipeline for dataset '{name}'")]
    public static partial IGenericMessage SavingPipeline(ILogger logger, string name);

    /// <summary>
    /// Logs that a pipeline was saved for the named dataset.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the pipeline that was saved.</param>
    /// <param name="name">The name of the dataset the pipeline belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Saved pipeline {id} for dataset '{name}'")]
    public static partial IGenericMessage SavedPipeline(ILogger logger, Guid id, string name);

    /// <summary>
    /// Logs that saving the pipeline for the named dataset failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose pipeline failed to save.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Warning,
        Message = "Failed to save pipeline for dataset '{name}'")]
    public static partial IGenericMessage SavePipelineFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception was thrown while saving the pipeline for the named dataset.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while saving the pipeline.</param>
    /// <param name="name">The name of the dataset whose pipeline failed to save.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Failed to save pipeline for dataset '{name}'")]
    public static partial IGenericMessage SavePipelineException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that no matching pipeline was found for the named dataset.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset for which no pipeline was found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "No matching pipeline found for dataset '{name}'")]
    public static partial IGenericMessage NoPipelineFound(ILogger logger, string name);

    /// <summary>
    /// Logs that the calculation graph for the named dataset is being compiled.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose calculation graph is being compiled.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Compiling calculation graph for dataset '{name}'")]
    public static partial IGenericMessage CompilingGraph(ILogger logger, string name);

    /// <summary>
    /// Logs that the calculation graph for the named dataset was compiled, with the resulting entity identifier.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose calculation graph was compiled.</param>
    /// <param name="id">The identifier of the compiled entity.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Compiled calculation graph for dataset '{name}' — entity id {id}")]
    public static partial IGenericMessage CompiledGraph(ILogger logger, string name, Guid id);

    /// <summary>
    /// Logs that compiling the calculation graph for the named dataset failed, with the reason.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose calculation graph failed to compile.</param>
    /// <param name="reason">The reason the compilation failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "Failed to compile calculation graph for dataset '{name}': {reason}")]
    public static partial IGenericMessage CompileGraphFailed(ILogger logger, string name, string reason);

    /// <summary>
    /// Logs that compilation for the named dataset was aborted because a required node type was not found in the graph.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose compilation was aborted.</param>
    /// <param name="nodeType">The required node type that was missing from the graph.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Compile aborted for dataset '{name}' — required node type '{nodeType}' not found in graph")]
    public static partial IGenericMessage CompileNodeMissing(ILogger logger, string name, string nodeType);

    /// <summary>
    /// Logs that compilation for the named dataset was aborted because a node is missing a required configuration key.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose compilation was aborted.</param>
    /// <param name="nodeType">The node type that is missing the required configuration key.</param>
    /// <param name="key">The required configuration key that is missing.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Compile aborted for dataset '{name}' — node '{nodeType}' missing required configuration key '{key}'")]
    public static partial IGenericMessage CompileConfigurationMissing(ILogger logger, string name, string nodeType, string key);

    /// <summary>
    /// Logs that compilation for the named dataset was cancelled.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the dataset whose compilation was cancelled.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Compile for dataset '{name}' was cancelled")]
    public static partial IGenericMessage CompileGraphCancelled(ILogger logger, string name);

    /// <summary>
    /// Logs that re-editing an existing calculated dataset's visual graph is not available because the
    /// draft designer store was retired and reverse projection from the calculation entity (calc-graph-on-canvas)
    /// is not yet implemented.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the calculated dataset requested for editing.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Warning,
        Message = "Editing the existing calculated dataset '{name}' graph is not available — the draft designer store was retired; rebuild the calculation, or re-create it")]
    public static partial IGenericMessage EditExistingNotAvailable(ILogger logger, string name);
}
