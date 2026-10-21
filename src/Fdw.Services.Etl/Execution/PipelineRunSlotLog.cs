using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// MessageLogging for the pipeline run slot (FDW-1511): a run that waits for the pipeline it started hands its slot back, and
/// takes it again when that pipeline ends.
/// EventId range: 20990-20991.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUN")]
public static partial class PipelineRunSlotLog
{
    /// <summary>Logs a run handing its slot back while it waits.</summary>
    [MessageLogging(EventId = 20990, Level = LogLevel.Debug, Message = "A pipeline run is waiting for a pipeline it started and hands its run slot back")]
    public static partial IGenericMessage SlotHandedBack(ILogger logger);

    /// <summary>Logs a run taking its slot again.</summary>
    [MessageLogging(EventId = 20991, Level = LogLevel.Debug, Message = "A pipeline run that waited takes its run slot again")]
    public static partial IGenericMessage SlotTakenAgain(ILogger logger);
}
