using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// MessageLogging for DataflowGraphConfigurationProvider gateway operations.
/// EventId range: 7433-7436
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS3")]
internal static partial class DataflowGraphConfigurationProviderLog
{
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace, Message = "Loading dataflow graph data (dataSets, dataStores, sources)")]
    public static partial IGenericMessage LoadTrace(ILogger logger);

    [MessageLogging(EventId = 11015, Level = LogLevel.Information, Message = "Dataflow graph data loaded: {dataSetCount} dataSets, {dataStoreCount} dataStores, {sourceCount} sources")]
    public static partial IGenericMessage Loaded(ILogger logger, int dataSetCount, int dataStoreCount, int sourceCount);

    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Pipeline '{pipelineName}' not found — returning 404")]
    public static partial IGenericMessage PipelineNotFound(ILogger logger, string pipelineName);

    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Failed to load pipeline list for filter check")]
    public static partial IGenericMessage PipelineFilterCheckFailed(ILogger logger, Exception ex);
}
