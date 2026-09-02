using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// What each endpoint and group put into the container, as the collect runs.
/// </summary>
/// <remarks>
/// The point of declaring endpoints rather than scanning for them is being able to answer "what got
/// registered, by whom" without attaching a debugger. Naming the endpoint is only half that answer:
/// the useful half is what it CONTRIBUTED, because an endpoint that registers a validator and one
/// that registers nothing look identical otherwise.
///
/// The count is the service-descriptor delta measured across the call, so it reports what actually
/// reached the container rather than what the body meant to add.
/// </remarks>
[MessageLoggingTypeCode("ENDPOINTREG")]
public static partial class EndpointRegistrationLog
{
    /// <summary>Logged for what a group registered in its own body, before its endpoints run.</summary>
    [MessageLogging(
        EventId = 91010,
        Level = LogLevel.Information,
        Message = "[{groupName}] group registered {serviceCount} service(s) of its own, then {endpointCount} endpoint(s)")]
    public static partial IGenericMessage GroupRegistered(
        ILogger logger,
        string groupName,
        int serviceCount,
        int endpointCount);

    /// <summary>Logged for what each endpoint registered.</summary>
    [MessageLogging(
        EventId = 91011,
        Level = LogLevel.Information,
        Message = "[{groupName}] {endpointName} registered {serviceCount} service(s) — {endpointType}")]
    public static partial IGenericMessage EndpointRegistered(
        ILogger logger,
        string groupName,
        string endpointName,
        int serviceCount,
        string endpointType);

    /// <summary>Logged for each endpoint the collect passes over.</summary>
    [MessageLogging(
        EventId = 91012,
        Level = LogLevel.Information,
        Message = "[{groupName}] SKIPPED {endpointName} — SkipRegistration is set on the endpoint")]
    public static partial IGenericMessage EndpointSkipped(ILogger logger, string groupName, string endpointName);

    /// <summary>Logged when a whole group is passed over.</summary>
    [MessageLogging(
        EventId = 91013,
        Level = LogLevel.Information,
        Message = "[{groupName}] SKIPPED entirely — SkipRegistration is set on the group")]
    public static partial IGenericMessage GroupSkipped(ILogger logger, string groupName);

    /// <summary>Logged when a host claims a group's phase to run itself, later.</summary>
    /// <remarks>Distinct from SKIPPED: a deferred phase is still going to run, at a position the
    /// host chose. Reading one as the other turns "runs later" into "never ran" at a glance.</remarks>
    [MessageLogging(
        EventId = 91015,
        Level = LogLevel.Information,
        Message = "[{groupName}] DEFERRED {phase} — the host claimed this phase and will run it itself")]
    public static partial IGenericMessage GroupDeferred(ILogger logger, string groupName, string phase);

    /// <summary>Logged when the registration chain completes having declared no endpoint at all.</summary>
    [MessageLogging(
        EventId = 91014,
        Level = LogLevel.Error,
        Message = "No endpoint declared itself. Every group registered without contributing an endpoint type, so there is nothing to hand FastEndpoints.")]
    public static partial IGenericMessage NoEndpointsDeclared(ILogger logger);

    // ── OpenAPI document processors ─────────────────────────────────────────────────────────────

    /// <summary>Logged as each document processor is attached to the OpenAPI document settings.</summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Trace,
        Message = "OpenAPI document processor '{processorType}' attached to document '{documentName}'")]
    public static partial IGenericMessage OpenApiProcessorAttached(ILogger logger, string processorType, string documentName);

    /// <summary>Logged once the Register phase has attached every document processor.</summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Debug,
        Message = "OpenAPI document '{documentName}' registered with {count} document processor(s): [{processorTypes}]")]
    public static partial IGenericMessage OpenApiProcessorsRegistered(ILogger logger, string documentName, int count, string processorTypes);

    /// <summary>Logged as each stateful processor receives the built service provider.</summary>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Trace,
        Message = "OpenAPI document processor '{processorType}' initialized with the built service provider")]
    public static partial IGenericMessage OpenApiProcessorInitialized(ILogger logger, string processorType);

    /// <summary>
    /// Logged when the Initialize phase runs with no processors to hand a provider to. Critical
    /// because the processors that need one are the filtering processors: without them the OpenAPI
    /// document is served unfiltered to every caller, including anonymous ones.
    /// </summary>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Critical,
        Message = "OpenAPI Initialize ran with NO document processors registered — the document will be served UNFILTERED to every caller, anonymous included. The Register phase did not attach them.")]
    public static partial IGenericMessage OpenApiProcessorsMissing(ILogger logger);

    /// <summary>Logs a host that declares no endpoint groups at all.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="phase">The phase being skipped.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Debug,
        Message = "No endpoint groups joined; this host serves no REST endpoints, so FastEndpoints {phase} is skipped")]
    public static partial IGenericMessage NoEndpointGroups(ILogger logger, string phase);

    /// <summary>Logs how many groups joined, before any is registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="groupCount">How many groups joined EndpointGroups.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Trace,
        Message = "Endpoint registration starting over {groupCount} joined group(s)")]
    public static partial IGenericMessage EndpointGroupsJoined(ILogger logger, int groupCount);

    /// <summary>Logs a single group about to register.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="group">The group type name.</param>
    /// <param name="memberCount">How many options the group holds.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11025,
        Level = LogLevel.Trace,
        Message = "Registering endpoint group {group} holding {memberCount} option(s)")]
    public static partial IGenericMessage EndpointGroupRegistering(ILogger logger, string group, int memberCount);

    /// <summary>Logs what a group actually contributed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="group">The group type name.</param>
    /// <param name="contributed">How many endpoint types it added.</param>
    /// <param name="runningTotal">Declared endpoints after this group.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Debug,
        Message = "Endpoint group {group} contributed {contributed} endpoint type(s); {runningTotal} declared so far")]
    public static partial IGenericMessage EndpointGroupContributed(ILogger logger, string group, int contributed, int runningTotal);

    /// <summary>Logs each middleware this collection adds to the request pipeline, in the order it adds it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ordinal">The position of this call within the sequence.</param>
    /// <param name="middleware">The middleware being added.</param>
    /// <param name="reason">Why it sits at this position.</param>
    [MessageLogging(EventId = 11027, Level = LogLevel.Debug,
        Message = "Request pipeline [{ordinal}]: {middleware} — {reason}")]
    public static partial IGenericMessage PipelineMiddlewareAdded(ILogger logger, int ordinal, string middleware, string reason);

    /// <summary>Logs the composed request pipeline once the collection has added every part of it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sequence">The middleware added, in order.</param>
    [MessageLogging(EventId = 31020, Level = LogLevel.Information,
        Message = "Request pipeline composed by the Endpoints collection: {sequence}")]
    public static partial IGenericMessage PipelineComposed(ILogger logger, string sequence);

    /// <summary>Logs the endpoint conventions applied to the FastEndpoints configuration.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ordinal">The position of this setting within the conventions.</param>
    /// <param name="setting">The configuration property being set.</param>
    /// <param name="value">The value it is set to.</param>
    /// <param name="reason">Why it is set to that.</param>
    [MessageLogging(EventId = 11028, Level = LogLevel.Debug,
        Message = "Endpoint convention [{ordinal}]: {setting} = {value} — {reason}")]
    public static partial IGenericMessage EndpointConventionApplied(ILogger logger, int ordinal, string setting, string value, string reason);

    /// <summary>Logs the single global attachment of the permission pre-processor.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 11029, Level = LogLevel.Debug,
        Message = "PermissionClaimsPreProcessor attached globally — every endpoint's Policies(resource:action) is checked, however the endpoint was declared")]
    public static partial IGenericMessage PermissionPreProcessorAttached(ILogger logger);
}
