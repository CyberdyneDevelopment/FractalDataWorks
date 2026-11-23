using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the offers of a pipeline (FDW-6135): the refusals of the offers service. Every refusal is logged and returned in the failure result, numbered in the category of what it
/// says: an offer that names both a person and a role is a request that is not valid (2), a pipeline or an offer that does not exist is not found (3), an offer that already stands conflicts (4), and
/// a configuration row that cannot be read or written is a dependency that failed (7). EventId range: 20890, 30891, 30892, 40893, 70894 and 70895.
/// </summary>
[MessageLoggingTypeCode("PIPELINEOFFER")]
public static partial class PipelineOfferLog
{
    /// <summary>Logs an offer that names both a person and a role.</summary>
    [MessageLogging(EventId = 20890, Level = LogLevel.Error,
        Message = "An offer of pipeline {pipelineId} names both a person and a role; an offer is to the whole tenant, to one person or to one role",
        ClientMessage = "Offer a pipeline to the whole tenant, to one person or to one role, not to a person and a role at once")]
    public static partial IGenericMessage NamesPersonAndRole(ILogger logger, Guid pipelineId);

    /// <summary>Logs offers asked for, or made, for a pipeline that does not exist in the caller's tenant.</summary>
    [MessageLogging(EventId = 30891, Level = LogLevel.Error,
        Message = "Pipeline {pipelineId} is not a pipeline of the caller's tenant, so it has no offers and none can be made",
        ClientMessage = "That pipeline does not exist")]
    public static partial IGenericMessage PipelineNotFound(ILogger logger, Guid pipelineId);

    /// <summary>Logs the removal of an offer that does not exist.</summary>
    [MessageLogging(EventId = 30892, Level = LogLevel.Error,
        Message = "Pipeline {pipelineId} has no offer {offerId} in the caller's tenant, so none was removed",
        ClientMessage = "That offer does not exist")]
    public static partial IGenericMessage OfferNotFound(ILogger logger, Guid pipelineId, Guid offerId);

    /// <summary>Logs an offer that already stands.</summary>
    [MessageLogging(EventId = 40893, Level = LogLevel.Error,
        Message = "Pipeline {pipelineId} is already offered in this way: to {to}",
        ClientMessage = "That pipeline is already offered in this way")]
    public static partial IGenericMessage AlreadyStands(ILogger logger, Guid pipelineId, string to);

    /// <summary>Logs offers that could not be read.</summary>
    [MessageLogging(EventId = 70894, Level = LogLevel.Error,
        Message = "The offers of pipeline {pipelineId} could not be read: {reason}")]
    public static partial IGenericMessage NotRead(ILogger logger, Guid pipelineId, string? reason);

    /// <summary>Logs an offer that could not be written or removed.</summary>
    [MessageLogging(EventId = 70895, Level = LogLevel.Error,
        Message = "An offer of pipeline {pipelineId} could not be written or removed: {reason}")]
    public static partial IGenericMessage NotWritten(ILogger logger, Guid pipelineId, string? reason);
}
