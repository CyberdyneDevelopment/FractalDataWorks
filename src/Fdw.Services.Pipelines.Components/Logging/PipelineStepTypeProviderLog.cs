using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Components.Logging;

/// <summary>
/// MessageLogging methods for PipelineBuilderProvider step type loading operations.
/// EventId range: 4266-4275
/// </summary>
[MessageLoggingTypeCode("COMPONENTS12")]
public static partial class PipelineStepTypeProviderLog
{
    /// <summary>Logs when pipeline step types are being loaded from the API.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace,
        Message = "PipelineBuilderProvider: Loading pipeline step types from configuration API")]
    public static partial IGenericMessage LoadingStepTypes(ILogger logger);

    /// <summary>Logs when pipeline step types have been loaded successfully.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information,
        Message = "PipelineBuilderProvider: Loaded {count} pipeline step types")]
    public static partial IGenericMessage LoadedStepTypes(ILogger logger, int count);

    /// <summary>Logs when pipeline step type loading fails (non-exception).</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "PipelineBuilderProvider: Failed to load pipeline step types from configuration API")]
    public static partial IGenericMessage LoadStepTypesFailed(ILogger logger);

    /// <summary>Logs when pipeline step type loading fails with an exception.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "PipelineBuilderProvider: Exception loading pipeline step types")]
    public static partial IGenericMessage LoadStepTypesException(ILogger logger, Exception exception);
}
