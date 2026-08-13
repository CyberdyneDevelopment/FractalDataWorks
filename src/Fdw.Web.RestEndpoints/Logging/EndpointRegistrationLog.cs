using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// What each endpoint and group put into the container, as the sweep runs.
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

    /// <summary>Logged for each endpoint the sweep passes over.</summary>
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

    /// <summary>Logged when the registration chain completes having declared no endpoint at all.</summary>
    [MessageLogging(
        EventId = 91014,
        Level = LogLevel.Error,
        Message = "No endpoint declared itself. Every group registered without contributing an endpoint type, so there is nothing to hand FastEndpoints.")]
    public static partial IGenericMessage NoEndpointsDeclared(ILogger logger);
}
