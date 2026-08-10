using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints.Logging;

/// <summary>
/// MessageLogging for Pipeline endpoint base operations.
/// EventId range: 7250
/// </summary>
/// <remarks>
/// Why: Relocated from 7139 to avoid collision with DataStoreEndpointLog and ConnectionProviderLogger.
/// The 7248-7260 range is reserved for configuration endpoint logs.
/// </remarks>
[MessageLoggingTypeCode("ENDPOINTS9")]
public static partial class PipelineEndpointLog
{
    /// <summary>Logs when a modification is rejected because the pipeline is a system configuration.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "Rejected modification of system pipeline '{pipelineName}' — system configurations are read-only")]
    public static partial IGenericMessage SystemPipelineReadOnly(ILogger logger, string pipelineName);

    /// <summary>Logs that a persisted pipeline is missing its required kind (ServiceOptionType).</summary>
    // Why: the kind discriminator is NOT NULL on pipe.Pipeline; a null is a data-integrity defect the list
    // endpoint fails loud on rather than substituting an "Unknown" display fallback.
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Pipeline '{pipelineName}' has no kind (ServiceOptionType)")]
    public static partial IGenericMessage PipelineMissingKind(ILogger logger, string pipelineName);
}
