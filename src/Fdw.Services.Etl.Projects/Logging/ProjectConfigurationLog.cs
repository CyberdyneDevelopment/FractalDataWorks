using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Logging;

/// <summary>
/// MessageLogging for ETL project orchestration configuration operations.
/// EventId range: 6532–6565 (extends Config Writers 6500–6531).
/// </summary>
[MessageLoggingTypeCode("PROJECTS")]
public static partial class ProjectConfigurationLog
{
    /// <summary>Validation failed before persisting a project, stage, or step configuration.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type that failed validation.</param>
    /// <param name="name">The name of the configuration that failed validation.</param>
    /// <param name="errors">The validation errors that were reported.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Warning,
        Message = "Validation failed for '{configurationType}' '{name}': {errors}")]
    public static partial IGenericMessage ValidationFailed(
        ILogger logger,
        string configurationType,
        string name,
        string errors);

    /// <summary>A policy elevation violation was detected — the child sets a less-strict value than the parent effective policy.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type on which the violation was detected.</param>
    /// <param name="name">The name of the configuration on which the violation was detected.</param>
    /// <param name="field">The policy field that was violated.</param>
    /// <param name="childValue">The child value that is less strict than the parent effective value.</param>
    /// <param name="parentValue">The parent effective value that the child value failed to meet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41006,
        Level = LogLevel.Warning,
        Message = "Policy elevation violation for '{configurationType}' '{name}': field '{field}' — child value '{childValue}' is less strict than parent effective '{parentValue}'")]
    public static partial IGenericMessage PolicyElevationViolation(
        ILogger logger,
        string configurationType,
        string name,
        string field,
        string childValue,
        string parentValue);

    /// <summary>A project configuration was not found by name or id.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="projectName">The name of the project that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31006,
        Level = LogLevel.Warning,
        Message = "Project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(
        ILogger logger,
        string projectName);

    /// <summary>A stage configuration was not found by name or id.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stageName">The name of the stage that was not found.</param>
    /// <param name="projectName">The name of the project the stage was searched in.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31007,
        Level = LogLevel.Warning,
        Message = "Stage '{stageName}' not found in project '{projectName}'")]
    public static partial IGenericMessage StageNotFound(
        ILogger logger,
        string stageName,
        string projectName);

    /// <summary>A step configuration was not found by name or id.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stepName">The name of the step that was not found.</param>
    /// <param name="stageName">The name of the stage the step was searched in.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31008,
        Level = LogLevel.Warning,
        Message = "Step '{stepName}' not found in stage '{stageName}'")]
    public static partial IGenericMessage StepNotFound(
        ILogger logger,
        string stepName,
        string stageName);

    /// <summary>A cycle was detected in the pipeline prerequisite graph for a step.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stepName">The name of the step where the prerequisite cycle was detected.</param>
    /// <param name="pipelineId">The id of the pipeline that is part of the cycle.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41007,
        Level = LogLevel.Error,
        Message = "Prerequisite cycle detected in step '{stepName}': pipeline '{pipelineId}' is part of a cycle")]
    public static partial IGenericMessage PrerequisiteCycleDetected(
        ILogger logger,
        string stepName,
        Guid pipelineId);

    /// <summary>A pipeline referenced in StepPipelineMembership does not exist.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stepName">The name of the step that holds the pipeline membership reference.</param>
    /// <param name="pipelineId">The id of the referenced pipeline that does not resolve to an existing pipeline.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31009,
        Level = LogLevel.Error,
        Message = "Pipeline membership reference '{pipelineId}' in step '{stepName}' does not resolve to an existing pipeline")]
    public static partial IGenericMessage PipelineMembershipMissing(
        ILogger logger,
        string stepName,
        Guid pipelineId);

    /// <summary>Persistence of a project configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the save to fail.</param>
    /// <param name="projectName">The name of the project that failed to save.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to save project '{projectName}': {error}")]
    public static partial IGenericMessage ProjectSaveFailed(
        ILogger logger,
        Exception exception,
        string projectName,
        string error);

    /// <summary>Persistence of a stage configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the save to fail.</param>
    /// <param name="stageName">The name of the stage that failed to save.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to save stage '{stageName}': {error}")]
    public static partial IGenericMessage StageSaveFailed(
        ILogger logger,
        Exception exception,
        string stageName,
        string error);

    /// <summary>Persistence of a step configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the save to fail.</param>
    /// <param name="stepName">The name of the step that failed to save.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Failed to save step '{stepName}': {error}")]
    public static partial IGenericMessage StepSaveFailed(
        ILogger logger,
        Exception exception,
        string stepName,
        string error);

    /// <summary>
    /// The ProjectServerDefaults appsettings section is missing or empty in a non-Development environment.
    /// This is a Critical failure — the server has no policy baseline.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Critical,
        Message = "ProjectServerDefaults appsettings section is missing or empty. Server policy defaults cannot be resolved. Ensure appsettings contains a 'ProjectServerDefaults' section.")]
    public static partial IGenericMessage ServerPolicyDefaultsMissing(
        ILogger logger);

    /// <summary>A resilience policy name set on a configuration does not resolve in the registry.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="resiliencePolicyName">The resilience policy name that did not resolve in the registry.</param>
    /// <param name="configurationType">The configuration type that referenced the resilience policy.</param>
    /// <param name="name">The name of the configuration that referenced the resilience policy.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31010,
        Level = LogLevel.Warning,
        Message = "Resilience policy name '{resiliencePolicyName}' on '{configurationType}' '{name}' is not null or whitespace but was not provided as a valid non-empty string")]
    public static partial IGenericMessage ResiliencePolicyNotFound(
        ILogger logger,
        string resiliencePolicyName,
        string configurationType,
        string name);

    /// <summary>A cross-tenant configuration reference was rejected because AllowCrossTenant is false.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type whose cross-tenant reference was rejected.</param>
    /// <param name="name">The name of the configuration whose cross-tenant reference was rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 51001,
        Level = LogLevel.Warning,
        Message = "Cross-tenant reference rejected for '{configurationType}' '{name}': AllowCrossTenant is false")]
    public static partial IGenericMessage CrossTenantNotAllowed(
        ILogger logger,
        string configurationType,
        string name);

    /// <summary>Deletion of a project configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the delete to fail.</param>
    /// <param name="projectId">The Id of the project that failed to delete.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "Failed to delete project with Id '{projectId}': {error}")]
    public static partial IGenericMessage ProjectDeleteFailed(
        ILogger logger,
        Exception exception,
        Guid projectId,
        string error);

    /// <summary>Deletion of a stage configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the delete to fail.</param>
    /// <param name="stageId">The Id of the stage that failed to delete.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "Failed to delete stage with Id '{stageId}': {error}")]
    public static partial IGenericMessage StageDeleteFailed(
        ILogger logger,
        Exception exception,
        Guid stageId,
        string error);

    /// <summary>Deletion of a step configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the delete to fail.</param>
    /// <param name="stepId">The Id of the step that failed to delete.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "Failed to delete step with Id '{stepId}': {error}")]
    public static partial IGenericMessage StepDeleteFailed(
        ILogger logger,
        Exception exception,
        Guid stepId,
        string error);

    /// <summary>Project provider loaded successfully with the given item count.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of projects that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Project provider loaded {count} project(s)")]
    public static partial IGenericMessage ProjectsLoaded(
        ILogger logger,
        int count);

    /// <summary>Stage provider loaded successfully with the given item count.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="projectId">The id of the project the stages were loaded for.</param>
    /// <param name="count">The number of stages that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "Stage provider loaded {count} stage(s) for project '{projectId}'")]
    public static partial IGenericMessage StagesLoaded(
        ILogger logger,
        Guid projectId,
        int count);

    /// <summary>Step provider loaded successfully with the given item count.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stageId">The id of the stage the steps were loaded for.</param>
    /// <param name="count">The number of steps that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Step provider loaded {count} step(s) for stage '{stageId}'")]
    public static partial IGenericMessage StepsLoaded(
        ILogger logger,
        Guid stageId,
        int count);

    /// <summary>A prerequisite pipeline reference does not belong to the same step.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stepName">The name of the step that holds the prerequisite pipeline reference.</param>
    /// <param name="prerequisitePipelineId">The id of the prerequisite pipeline that is not a member of the same step.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Error,
        Message = "Prerequisite pipeline '{prerequisitePipelineId}' in step '{stepName}' is not a member of the same step")]
    public static partial IGenericMessage PrerequisiteNotMemberOfStep(
        ILogger logger,
        string stepName,
        Guid prerequisitePipelineId);

    /// <summary>Project save succeeded.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="projectName">The name of the project that was saved.</param>
    /// <param name="projectId">The Id assigned to the saved project.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Project '{projectName}' saved successfully with Id '{projectId}'")]
    public static partial IGenericMessage ProjectSaved(
        ILogger logger,
        string projectName,
        Guid projectId);

    /// <summary>Stage save succeeded.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stageName">The name of the stage that was saved.</param>
    /// <param name="stageId">The Id assigned to the saved stage.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "Stage '{stageName}' saved successfully with Id '{stageId}'")]
    public static partial IGenericMessage StageSaved(
        ILogger logger,
        string stageName,
        Guid stageId);

    /// <summary>Step save succeeded.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stepName">The name of the step that was saved.</param>
    /// <param name="stepId">The Id assigned to the saved step.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Information,
        Message = "Step '{stepName}' saved successfully with Id '{stepId}'")]
    public static partial IGenericMessage StepSaved(
        ILogger logger,
        string stepName,
        Guid stepId);

    /// <summary>Policy elevation validation failed with one or more violations.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type that failed policy elevation validation.</param>
    /// <param name="name">The name of the configuration that failed policy elevation validation.</param>
    /// <param name="violations">The policy elevation violations that were reported.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41008,
        Level = LogLevel.Warning,
        Message = "Policy elevation validation failed for '{configurationType}' '{name}': {violations}")]
    public static partial IGenericMessage PolicyElevationFailed(
        ILogger logger,
        string configurationType,
        string name,
        string violations);
}
