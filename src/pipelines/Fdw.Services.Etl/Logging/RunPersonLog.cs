using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// Messages of the person a pipeline run acts as (FDW-6137): the run that acts as its person, and each reason the person is refused, each with its own
/// number. EventId range: 10836-10837, 60836, 60838, 70836-70837, 100831-100833.
/// </summary>
[MessageLoggingTypeCode("RUNPERSON")]
public static partial class RunPersonLog
{
    /// <summary>Logs a run whose work acts as its person, narrowed to its roles.</summary>
    [MessageLogging(EventId = 10836, Level = LogLevel.Information,
        Message = "Run '{executionId}' of pipeline '{pipeline}' acts as person '{personId}' in tenant '{tenantId}' with the roles {roles}")]
    public static partial IGenericMessage RunActsAsPerson(ILogger logger, Guid executionId, string pipeline, Guid personId, Guid tenantId, string roles);

    /// <summary>Logs a run whose identity was not resolved because its token was cancelled: nothing is read and the run does not start.</summary>
    [MessageLogging(EventId = 10837, Level = LogLevel.Warning,
        Message = "Run '{executionId}' of pipeline '{pipeline}' was cancelled before its identity was resolved")]
    public static partial IGenericMessage RunIdentityCancelled(ILogger logger, Guid executionId, string pipeline);

    /// <summary>Logs a person's run on a host that registers no eligibility check or tenant provider: the person cannot be judged, so the run is refused.</summary>
    [MessageLogging(EventId = 60836, Level = LogLevel.Critical,
        Message = "Run '{executionId}' of pipeline '{pipeline}' is refused: it acts as person '{personId}', and this host registers no IIssuanceEligibility or ITenantProvider to judge them")]
    public static partial IGenericMessage RunPersonCannotBeJudged(ILogger logger, Guid executionId, string pipeline, Guid personId);

    /// <summary>
    /// Logs a service the judge of a person's run needs that a host which runs pipelines has not registered. Raised once, when the host initializes, so the host does not
    /// start; the per-run <see cref="RunPersonCannotBeJudged"/> is the backstop for a host that skips the check (FDW-6528). The number is in the Configuration category (6).
    /// </summary>
    [MessageLogging(EventId = 60838, Level = LogLevel.Critical,
        Message = "This host runs pipelines and registers no {service}, which judges whether the person a run acts as may act in its tenant, so the host does not start",
        ClientMessage = "A service that judges a person's pipeline run is not registered")]
    public static partial IGenericMessage JudgeServiceNotRegistered(ILogger logger, string service);

    /// <summary>Logs a run whose person's sign-in eligibility cannot be read: the person cannot be judged, so the run is refused.</summary>
    [MessageLogging(EventId = 70836, Level = LogLevel.Error,
        Message = "Run '{executionId}' of pipeline '{pipeline}' is refused: whether person '{personId}' may act in tenant '{tenantId}' could not be read: {reason}")]
    public static partial IGenericMessage RunPersonEligibilityUnreadable(ILogger logger, Guid executionId, string pipeline, Guid personId, Guid tenantId, string? reason);

    /// <summary>Logs a person's run whose tenant cannot be read, so whether it is the global tenant is unknown and the run is refused.</summary>
    [MessageLogging(EventId = 70837, Level = LogLevel.Error,
        Message = "Run '{executionId}' of pipeline '{pipeline}' is refused: tenant '{tenantId}' of person '{personId}' could not be read: {reason}")]
    public static partial IGenericMessage RunPersonTenantUnreadable(ILogger logger, Guid executionId, string pipeline, Guid personId, Guid tenantId, string? reason);

    /// <summary>Logs a run whose person may no longer act in the run's tenant (deleted, disabled, locked out or not a member), naming the reason.</summary>
    [MessageLogging(EventId = 100833, Level = LogLevel.Error,
        Message = "Run '{executionId}' of pipeline '{pipeline}' is refused: person '{personId}' may not act in tenant '{tenantId}': {reason}")]
    public static partial IGenericMessage RunPersonIneligible(ILogger logger, Guid executionId, string pipeline, Guid personId, Guid tenantId, string reason);

    /// <summary>Logs a run whose person's permissions in the run's tenant cannot be read: the person is refused, never run as nobody.</summary>
    [MessageLogging(EventId = 100831, Level = LogLevel.Error,
        Message = "Run '{executionId}' of pipeline '{pipeline}' is refused: the permissions of person '{personId}' in tenant '{tenantId}' could not be read: {reason}")]
    public static partial IGenericMessage RunPersonNotPermitted(ILogger logger, Guid executionId, string pipeline, Guid personId, Guid tenantId, string? reason);

    /// <summary>Logs a run whose person no longer holds what the run's roles carry.</summary>
    [MessageLogging(EventId = 100832, Level = LogLevel.Error,
        Message = "Run '{executionId}' of pipeline '{pipeline}' is refused: person '{personId}' no longer holds what the run's roles carry: {permissions}")]
    public static partial IGenericMessage RunPersonRolesNotHeld(ILogger logger, Guid executionId, string pipeline, Guid personId, string permissions);
}
