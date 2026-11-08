using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The coded messages the pipelines endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("PIPELINESANSWERS")]
public static partial class PipelinesAnswerLog
{

    /// <summary>Logs that the request failed: Name exactly one of 'id' or 'name'.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning, Message = "Name exactly one of 'id' or 'name'.", ClientMessage = "Name exactly one of 'id' or 'name'.")]
    public static partial IGenericMessage GetPipelineDetailNameExactlyOneOfId(ILogger logger);

    /// <summary>Logs that Script was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Script was not found, or is not visible to the caller", ClientMessage = "Script was not found")]
    public static partial IGenericMessage RunScriptNotFound(ILogger logger);

    /// <summary>Logs that the request failed: The run queue is listed for the caller's tenant, and this request carries none..</summary>
    [MessageLogging(EventId = 100001, Level = LogLevel.Warning, Message = "The run queue is listed for the caller's tenant, and this request carries none.", ClientMessage = "The run queue is listed for the caller's tenant, and this request carries none.")]
    public static partial IGenericMessage ListPipelineRunQueueTheRunQueueIsListed(ILogger logger);

    /// <summary>Logs that Describing a graph answered a success and no value.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "Describing a graph answered a success and no value", ClientMessage = "The graph could not be described")]
    public static partial IGenericMessage DescribeGraphDescribeReturnedNothing(ILogger logger);
}
