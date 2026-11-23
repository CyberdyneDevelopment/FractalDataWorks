using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Logging;

/// <summary>
/// Messages of the Pipeline domain's service type collection, <see cref="PipelineTypes"/> (FDW-6244).
/// EventId range: 60833-60835, 60837 (60836 is RUNPERSON's, in Fdw.Services.Etl).
/// </summary>
[MessageLoggingTypeCode("PIPELINES")]
public static partial class PipelineTypesLog
{
    /// <summary>
    /// Logs a host that attached a pipeline type but registered no pipe.Pipeline configuration provider, which every implementation row hangs from. The
    /// number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60833, Level = LogLevel.Critical,
        Message = "The host attached a pipeline type but registered no IPipelineConfigurationProvider, which PipelineTypes registers; the host does not start",
        ClientMessage = "A pipeline type needs the pipeline configuration provider to be registered, and it is not")]
    public static partial IGenericMessage ConfigurationProviderNotRegistered(ILogger logger);

    /// <summary>
    /// Logs a pipeline type whose run mode is none of the <c>PipelineRunModes</c> options, found when the host starts so no run of it is ever started. The
    /// number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60834, Level = LogLevel.Critical,
        Message = "Pipeline type {pipelineType} names run mode {runModeName}, which no PipelineRunModes option declares; the host does not start",
        ClientMessage = "A pipeline type names a run mode that does not exist")]
    public static partial IGenericMessage RunModeNotDeclared(ILogger logger, string pipelineType, string runModeName);

    /// <summary>
    /// Logs a run mode no runner registered in a host that runs pipelines carries out, found when the host starts instead of at the first start of a run
    /// of that pipeline type. The number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60835, Level = LogLevel.Critical,
        Message = "Pipeline type {pipelineType} runs {runModeName}, and no IPipelineRunner registered in this host carries out that run mode; the host does not start",
        ClientMessage = "This host runs pipelines but has no runner for a pipeline type's run mode")]
    public static partial IGenericMessage RunModeHasNoRunner(ILogger logger, string pipelineType, string runModeName);

    /// <summary>
    /// Logs a pipeline type that, once every option has initialized, has no row provider registered under its name on the pipe.Pipeline domain provider,
    /// so a pipeline whose row names it could not be read; a type that sets no phase is one. The number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60837, Level = LogLevel.Critical,
        Message = "Pipeline type {pipelineType} registered no row provider under its name on the pipe.Pipeline domain provider by the end of Initialize; a pipeline that names it cannot be read, and the host does not start",
        ClientMessage = "A pipeline type has no configuration provider for its pipelines")]
    public static partial IGenericMessage ImplementationProviderNotRegistered(ILogger logger, string pipelineType);

    /// <summary>
    /// Logs a pipeline type that requires a fact of its configuration which its configuration type cannot state, found when the host starts (FDW-6141). The number
    /// is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60839, Level = LogLevel.Critical,
        Message = "Pipeline type {pipelineType} requires its pipelines to state {fact}, and its configuration type does not implement {configurationInterface}, so a pipeline of the type cannot state it; the host does not start",
        ClientMessage = "A pipeline type requires a fact its configuration cannot state")]
    public static partial IGenericMessage ConfigurationCannotStateRequirement(ILogger logger, string pipelineType, string fact, string configurationInterface);

    /// <summary>Logs a pipeline whose row leaves out a fact its pipeline type requires (FDW-6141). The number is in the Validation category (2): the row is what is wrong, so the save answers HTTP 400 (FDW-7013 moved it from 60840).</summary>
    [MessageLogging(EventId = 20840, Level = LogLevel.Error,
        Message = "A pipeline of type {pipelineType} must state {fact}, and does not, so it is not saved",
        ClientMessage = "A pipeline of this type must state a value it leaves out")]
    public static partial IGenericMessage RequiredFactNotStated(ILogger logger, string pipelineType, string fact);

    /// <summary>
    /// Logs a host that attached a pipeline type but registered no <c>IDescriptionWriter</c>, which the pipeline provider writes each pipeline's and parameter's description with
    /// in the unit of the save (FDW-7323). The number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60860, Level = LogLevel.Critical,
        Message = "The host attached a pipeline type but registered no IDescriptionWriter, which the pipeline provider writes each pipeline's and parameter's description with in the unit of the save; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "A pipeline type needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage DescriptionWriterNotRegistered(ILogger logger);

    /// <summary>
    /// Logs a host that attached a pipeline service type (scripts, webhook triggers, orchestration nodes) but registered no <c>IDescriptionWriter</c>, which their providers write
    /// each description with in the unit of the save (FDW-7323). The number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60861, Level = LogLevel.Critical,
        Message = "The host attached a pipeline service type but registered no IDescriptionWriter, which the script, webhook trigger and orchestration node providers write each description with in the unit of the save; register the Descriptions service type (Fdw.Services.Descriptions, DescriptionServiceTypes); the host does not start",
        ClientMessage = "A pipeline service type needs the Descriptions service type to be registered, and it is not")]
    public static partial IGenericMessage ServiceDescriptionWriterNotRegistered(ILogger logger);
}
