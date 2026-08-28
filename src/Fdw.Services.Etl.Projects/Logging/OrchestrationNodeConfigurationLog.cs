using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Logging;

/// <summary>
/// MessageLogging for ETL orchestration node configuration operations.
/// EventId range: 6532–6565 (reuses same range as the v1 ProjectConfigurationLog — same domain).
/// </summary>
/// <remarks>
/// This class replaces <see cref="ProjectConfigurationLog"/> for all new orchestration node operations.
/// ProjectConfigurationLog is preserved as an [Obsolete] alias during the transition release.
/// </remarks>
[MessageLoggingTypeCode("PROJECTS")]
public static partial class OrchestrationNodeConfigurationLog
{
    /// <summary>Validation failed before persisting a node configuration.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type that failed validation.</param>
    /// <param name="name">The name of the configuration that failed validation.</param>
    /// <param name="errors">The validation errors that were reported.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Validation failed for '{configurationType}' '{name}': {errors}")]
    public static partial IGenericMessage ValidationFailed(
        ILogger logger,
        string configurationType,
        string name,
        string errors);

    /// <summary>A policy elevation violation was detected.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type on which the violation was detected.</param>
    /// <param name="name">The name of the configuration on which the violation was detected.</param>
    /// <param name="field">The policy field that was violated.</param>
    /// <param name="childValue">The child value that is less strict than the parent effective value.</param>
    /// <param name="parentValue">The parent effective value that the child value failed to meet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "Policy elevation violation for '{configurationType}' '{name}': field '{field}' — child value '{childValue}' is less strict than parent effective '{parentValue}'")]
    public static partial IGenericMessage PolicyElevationViolation(
        ILogger logger,
        string configurationType,
        string name,
        string field,
        string childValue,
        string parentValue);

    /// <summary>A node was not found by name.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeName">The name of the orchestration node that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "OrchestrationNode '{nodeName}' not found")]
    public static partial IGenericMessage NodeNotFound(
        ILogger logger,
        string nodeName);

    /// <summary>A node was not found by logical id.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeId">The logical id of the orchestration node that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Warning,
        Message = "OrchestrationNode with id '{nodeId}' not found")]
    public static partial IGenericMessage NodeNotFoundById(
        ILogger logger,
        Guid nodeId);

    /// <summary>A cycle was detected in the pipeline prerequisite graph for a node.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeName">The name of the node where the prerequisite cycle was detected.</param>
    /// <param name="pipelineId">The id of the pipeline that is part of the cycle.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41001,
        Level = LogLevel.Error,
        Message = "Prerequisite cycle detected in node '{nodeName}': pipeline '{pipelineId}' is part of a cycle")]
    public static partial IGenericMessage PrerequisiteCycleDetected(
        ILogger logger,
        string nodeName,
        Guid pipelineId);

    /// <summary>A pipeline referenced in a membership does not exist.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeName">The name of the node that holds the pipeline membership reference.</param>
    /// <param name="pipelineId">The id of the referenced pipeline that does not resolve to an existing pipeline.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31002,
        Level = LogLevel.Error,
        Message = "Pipeline membership reference '{pipelineId}' in node '{nodeName}' does not resolve to an existing pipeline")]
    public static partial IGenericMessage PipelineMembershipMissing(
        ILogger logger,
        string nodeName,
        Guid pipelineId);

    /// <summary>Persistence of a node configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the save to fail.</param>
    /// <param name="nodeName">The name of the orchestration node that failed to save.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to save orchestration node '{nodeName}': {error}")]
    public static partial IGenericMessage NodeSaveFailed(
        ILogger logger,
        Exception exception,
        string nodeName,
        string error);

    /// <summary>
    /// The ProjectServerDefaults appsettings section is missing or empty in a non-Development environment.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Critical,
        Message = "ProjectServerDefaults appsettings section is missing or empty. Server policy defaults cannot be resolved.")]
    public static partial IGenericMessage ServerPolicyDefaultsMissing(
        ILogger logger);

    /// <summary>A cross-tenant configuration reference was rejected because AllowCrossTenant is false.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type whose cross-tenant reference was rejected.</param>
    /// <param name="name">The name of the configuration whose cross-tenant reference was rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "Cross-tenant reference rejected for '{configurationType}' '{name}': AllowCrossTenant is false")]
    public static partial IGenericMessage CrossTenantNotAllowed(
        ILogger logger,
        string configurationType,
        string name);

    /// <summary>Deletion of a node configuration failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the delete to fail.</param>
    /// <param name="nodeId">The Id of the orchestration node that failed to delete.</param>
    /// <param name="error">The error detail describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to delete orchestration node with Id '{nodeId}': {error}")]
    public static partial IGenericMessage NodeDeleteFailed(
        ILogger logger,
        Exception exception,
        Guid nodeId,
        string error);

    /// <summary>Node provider loaded successfully with the given item count.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of orchestration nodes that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "OrchestrationNode provider loaded {count} node(s)")]
    public static partial IGenericMessage NodesLoaded(
        ILogger logger,
        int count);

    /// <summary>Node save succeeded.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeName">The name of the orchestration node that was saved.</param>
    /// <param name="nodeId">The Id assigned to the saved orchestration node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "OrchestrationNode '{nodeName}' saved successfully with Id '{nodeId}'")]
    public static partial IGenericMessage NodeSaved(
        ILogger logger,
        string nodeName,
        Guid nodeId);

    /// <summary>Policy elevation validation failed with one or more violations.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="configurationType">The configuration type that failed policy elevation validation.</param>
    /// <param name="name">The name of the configuration that failed policy elevation validation.</param>
    /// <param name="violations">The policy elevation violations that were reported.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41002,
        Level = LogLevel.Warning,
        Message = "Policy elevation validation failed for '{configurationType}' '{name}': {violations}")]
    public static partial IGenericMessage PolicyElevationFailed(
        ILogger logger,
        string configurationType,
        string name,
        string violations);

    /// <summary>A NodeTypeId does not resolve to a known OrchestrationNodeType.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeTypeId">The NodeTypeId that does not resolve to a registered OrchestrationNodeType.</param>
    /// <param name="nodeName">The name of the node carrying the unresolved NodeTypeId.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31003,
        Level = LogLevel.Error,
        Message = "NodeTypeId '{nodeTypeId}' on node '{nodeName}' does not resolve to any registered OrchestrationNodeType")]
    public static partial IGenericMessage NodeTypeNotFound(
        ILogger logger,
        int nodeTypeId,
        string nodeName);

    /// <summary>A child node type is not in the parent's AllowedChildTypeNames.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="childTypeName">The child node type that is not allowed under the parent type.</param>
    /// <param name="parentTypeName">The parent node type whose AllowedChildTypeNames excludes the child type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41003,
        Level = LogLevel.Error,
        Message = "Node type '{childTypeName}' is not allowed as a child of parent type '{parentTypeName}'")]
    public static partial IGenericMessage ChildTypeNotAllowed(
        ILogger logger,
        string childTypeName,
        string parentTypeName);

    /// <summary>A cycle was detected in the node hierarchy (node is its own transitive ancestor).</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeId">The id of the node that is a transitive ancestor of itself.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 41004,
        Level = LogLevel.Error,
        Message = "Cycle detected: node '{nodeId}' is a transitive ancestor of itself")]
    public static partial IGenericMessage NodeHierarchyCycleDetected(
        ILogger logger,
        Guid nodeId);

    /// <summary>A non-root node was saved without a ParentRowId.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="nodeName">The name of the non-root node that is missing a ParentRowId.</param>
    /// <param name="nodeTypeName">The type name of the node (CanBeRoot=false) that requires a non-null ParentRowId.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "Node '{nodeName}' with type '{nodeTypeName}' (CanBeRoot=false) must have a non-null ParentRowId")]
    public static partial IGenericMessage NonRootNodeMissingParent(
        ILogger logger,
        string nodeName,
        string nodeTypeName);
}
