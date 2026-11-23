using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Messages of the pipeline type a graph pipeline states at a save (FDW-6718). EventId range: 20846, 20847, 20848, 20850 and 20854 (Validation, 2: the caller's request is what is wrong, so each answers HTTP 400; FDW-7013 moved them from 60846 to 60850, whose
/// Configuration category answered 500) and the draft's warning 60849, which is no refusal. The requirements of a type, 60839 and 20840, are <c>PipelineTypesLog</c>'s.
/// </summary>
[MessageLoggingTypeCode("PIPELINES")]
public static partial class GraphImplementationLog
{
    /// <summary>Logs a create that states no pipeline type, or a stored template that states none; nothing stands in for it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="subject">What states no type: the request or the template's name.</param>
    /// <returns>The structured message the refused save carries.</returns>
    [MessageLogging(EventId = 20846, Level = LogLevel.Error,
        Message = "{subject} states no pipeline implementation (the type of the pipeline), and none is assumed for it, so the pipeline is not saved",
        ClientMessage = "State the pipeline's implementation: its type")]
    public static partial IGenericMessage ImplementationNotStated(ILogger logger, string subject);

    /// <summary>Logs a stated implementation that is not a registered pipeline type closing the graph base.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="implementation">What the request states.</param>
    /// <param name="offered">The graph pipeline types the host registers.</param>
    /// <returns>The structured message the refused save carries.</returns>
    [MessageLogging(EventId = 20847, Level = LogLevel.Error,
        Message = "'{implementation}' is not a pipeline type this host registers that saves a graph; they are: {offered}",
        ClientMessage = "That is not a pipeline type that saves a graph")]
    public static partial IGenericMessage ImplementationNotRegistered(ILogger logger, string implementation, string offered);

    /// <summary>Logs an update, or a create from a template, that states a different type from the stored one: a pipeline's type does not change in a save.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stated">The implementation the request states.</param>
    /// <param name="stored">The implementation stored.</param>
    /// <returns>The structured message the refused save carries.</returns>
    [MessageLogging(EventId = 20848, Level = LogLevel.Error,
        Message = "The request states implementation '{stated}', and the stored pipeline or template is '{stored}'; changing a pipeline's type is not part of a save",
        ClientMessage = "A pipeline's type cannot be changed by a save")]
    public static partial IGenericMessage ImplementationChanged(ILogger logger, string stated, string stored);

    /// <summary>Logs, as a note on a draft that is otherwise valid, that no type was stated so the requirements of the type were not judged.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message the draft's answer carries.</returns>
    [MessageLogging(EventId = 60849, Level = LogLevel.Warning,
        Message = "The draft states no implementation, so the values its pipeline type requires were not judged; a create is refused when any is missing",
        ClientMessage = "Valid so far: the requirements of the pipeline type were not judged, because no type is stated")]
    public static partial IGenericMessage TypeRequirementsNotJudged(ILogger logger);

    /// <summary>Logs a save that states time or step limits for a pipeline that runs at once, by a caller who acts in no tenant: the ceilings are a tenant's settings, so nothing could judge them.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <returns>The structured message the refused save carries.</returns>
    [MessageLogging(EventId = 20850, Level = LogLevel.Error,
        Message = "Pipeline {pipelineName} states limits and runs at once, and the caller acts in no tenant, so the limits cannot be judged against a ceiling and the pipeline is not saved; act in a tenant and save it again",
        ClientMessage = "Save this pipeline while acting in a tenant: its limits are judged against that tenant's ceilings")]
    public static partial IGenericMessage LimitsNeedATenant(ILogger logger, string pipelineName);
    /// <summary>Logs a save that states a half-done rule that is no option of <c>HalfDoneRules</c> (FDW-6986): the rule is the caller's statement, so it is refused and no other rule stands in for it. Numbered in the Validation category (2): the request is what is wrong.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="rule">The rule the save states.</param>
    /// <param name="offered">The rules that are options, comma separated.</param>
    /// <returns>The structured message the refused save carries.</returns>
    [MessageLogging(EventId = 20854, Level = LogLevel.Error,
        Message = "The half-done rule {rule} is not one of the rules offered ({offered}), so the pipeline is not saved and no other rule stands in for it",
        ClientMessage = "State one of the offered half-done rules: what happens to a run found half-done after a restart")]
    public static partial IGenericMessage HalfDoneRuleNotAnOption(ILogger logger, string rule, string offered);

    /// <summary>Logs a save of a graph that nests components that hold components, when the host does not set how deep they may nest (FDW-6134): no depth is assumed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="setting">The setting that is not set to a positive number.</param>
    /// <returns>The structured message the refused save carries.</returns>
    [MessageLogging(EventId = 20855, Level = LogLevel.Error,
        Message = "Pipeline {pipelineName} nests components that hold components, and the setting {setting} is not set to a positive number, so its depth cannot be judged and the pipeline is not saved; no depth is assumed",
        ClientMessage = "This pipeline is not saved: the host does not say how deep components may nest")]
    public static partial IGenericMessage ContainerDepthNotSet(ILogger logger, string pipelineName, string setting);

    /// <summary>Logs a save of a graph whose components that hold components nest deeper than the setting allows (FDW-6134).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="component">The innermost component that holds components.</param>
    /// <param name="depth">How deep it nests.</param>
    /// <param name="maximum">The most the setting allows.</param>
    /// <returns>The structured message the refused save carries.</returns>
    [MessageLogging(EventId = 20856, Level = LogLevel.Error,
        Message = "Component {component} of pipeline {pipelineName} nests {depth} levels of components that hold components, and at most {maximum} are allowed, so the pipeline is not saved",
        ClientMessage = "Nest fewer levels of containers and loops: the most this host allows is shown in the message")]
    public static partial IGenericMessage ContainerTooDeep(ILogger logger, string pipelineName, string component, int depth, int maximum);
}
