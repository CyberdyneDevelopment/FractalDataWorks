using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the publisher of the components of runs in flight (FDW-3778).
/// EventId range: 60313, 71312.
/// </summary>
[MessageLoggingTypeCode("ETL")]
public static partial class PipelineComponentsPublisherLog
{
    /// <summary>Logs, once at host start, that the publish interval has no usable value, so no components are published.</summary>
    [MessageLogging(EventId = 60313, Level = LogLevel.Error, Message = "Pipeline components are not published: setting '{setting}' has no positive value, and no interval is assumed")]
    public static partial IGenericMessage IntervalNotSet(ILogger logger, string setting);

    /// <summary>Logs a publish that threw for one run; the next tick tries again.</summary>
    [MessageLogging(EventId = 71312, Level = LogLevel.Error, Message = "Publishing the components of execution {executionId} threw")]
    public static partial IGenericMessage PublishFailed(ILogger logger, Exception exception, Guid executionId);
}
