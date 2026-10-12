using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Abstractions.Logging;

/// <summary>MessageLogging for resolving the pipeline a schedule names, in the saver's view (FDW-1088).</summary>
[MessageLoggingTypeCode("SCHEDULEPIPELINE")]
public static partial class SchedulePipelineLog
{
    /// <summary>Logs a schedule refused because the caller cannot see the pipeline it names.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}' names pipeline '{pipelineId}', which does not exist or is not visible to the caller")]
    public static partial IGenericMessage SchedulePipelineNotFound(ILogger logger, string scheduleName, Guid pipelineId);

    /// <summary>Logs a pipeline lookup that reported success but returned no pipeline.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Schedule '{scheduleName}': looking up pipeline '{pipelineId}' succeeded but returned no pipeline")]
    public static partial IGenericMessage SchedulePipelineLookupEmpty(ILogger logger, string scheduleName, Guid pipelineId);
}
