using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Workflows.Abstractions;
using Fdw.Services.Workflows.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Workflows;
/// <summary>
/// Logging methods for WorkflowProvider.
/// EventId range: 7850-7869
/// </summary>
[MessageLoggingTypeCode("WORKFLOW")]
public static partial class WorkflowProviderLog
{
    /// <summary>Logs when the workflow index is rebuilt.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Workflow index rebuilt: {workflowCount} workflows")]
    public static partial IGenericMessage WorkflowIndexRebuilt(ILogger logger, int workflowCount);
    /// <summary>Logs when a workflow is retrieved by ID.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Workflow retrieved by ID '{id}' from {source}")]
    public static partial IGenericMessage WorkflowRetrievedById(ILogger logger, Guid id, string source);
    /// <summary>Logs when a workflow is retrieved by name.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Workflow retrieved by name '{name}' from {source}")]
    public static partial IGenericMessage WorkflowRetrievedByName(ILogger logger, string name, string source);
    /// <summary>Logs when a workflow is not found by ID.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Workflow with ID '{id}' not found")]
    public static partial IGenericMessage WorkflowByIdNotFound(ILogger logger, Guid id);
    /// <summary>Logs when a workflow is not found by name.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Workflow '{name}' not found")]
    public static partial IGenericMessage WorkflowByNameNotFound(ILogger logger, string name);
    /// <summary>Logs when all workflows are retrieved.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Retrieved {count} workflows")]
    public static partial IGenericMessage AllWorkflowsRetrieved(ILogger logger, int count);
    /// <summary>Logs when a workflow is registered.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Registered workflow '{name}'")]
    public static partial IGenericMessage WorkflowRegistered(ILogger logger, string name);
    /// <summary>Logs when a workflow is unregistered.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Unregistered workflow '{name}'")]
    public static partial IGenericMessage WorkflowUnregistered(ILogger logger, string name);
    /// <summary>Logs when configuration change is detected.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Configuration change detected, rebuilding workflow index")]
    public static partial IGenericMessage ConfigurationChangeDetected(ILogger logger);
}
