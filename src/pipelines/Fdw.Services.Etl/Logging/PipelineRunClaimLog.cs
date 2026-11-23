using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the durable claim of a pipeline run (FDW-6629): the keys a run claims, the lease its holder fences itself with, the four settings and the answers of the claim procedures.
/// Every refusal is logged and returned in the failure result, and none is completed with a guess.
/// EventId range: 60860-60877; the refusals a caller meets through the hold routes carry the number of the category whose HTTP status fits them (the number divided by 10000 picks the status): 20876 and 20879 answer 400, 30875 answers 404, 40874 answers 409, 50878 answers 401.
/// </summary>
[MessageLoggingTypeCode("PIPELINERUNCLAIM")]
public static partial class PipelineRunClaimLog
{
    /// <summary>Logs a claim setting that is not set or not usable.</summary>
    [MessageLogging(EventId = 60860, Level = LogLevel.Error,
        Message = "The claim setting {setting} is not usable: {reason}. The claim service does not start, and no number is assumed")]
    public static partial IGenericMessage SettingNotUsable(ILogger logger, string setting, string reason);

    /// <summary>Logs a stated key that has no text.</summary>
    [MessageLogging(EventId = 60861, Level = LogLevel.Error, Message = "A stated claim key has no text: the pipeline's template expanded to nothing, so the run claims no key it can name")]
    public static partial IGenericMessage StatedKeyTextMissing(ILogger logger);

    /// <summary>Logs a stated key built for no tenant.</summary>
    [MessageLogging(EventId = 60862, Level = LogLevel.Error, Message = "A stated claim key is built for no tenant: a stated key always begins with the tenant's id, so it cannot be built without one")]
    public static partial IGenericMessage TenantMissing(ILogger logger);

    /// <summary>Logs a write key with a part missing.</summary>
    [MessageLogging(EventId = 60863, Level = LogLevel.Error, Message = "A write claim key has no {part}: the connection, the path and the container are each needed to name what the run writes")]
    public static partial IGenericMessage WriteKeyPartMissing(ILogger logger, string part);

    /// <summary>Logs a cap below one.</summary>
    [MessageLogging(EventId = 60864, Level = LogLevel.Error, Message = "The cap of a pipeline is {cap}: it is at least 1, or no run of the pipeline could be claimed")]
    public static partial IGenericMessage CapNotPositive(ILogger logger, int cap);

    /// <summary>Logs a claim call made by a host that names no operational connection.</summary>
    [MessageLogging(EventId = 60865, Level = LogLevel.Error,
        Message = "The host names no operational connection, so {operation} cannot reach the claim table: PipelineTypes.OperationalConnection is not set")]
    public static partial IGenericMessage OperationalConnectionNotNamed(ILogger logger, string operation);

    /// <summary>Logs a procedure that answered no state for the run.</summary>
    [MessageLogging(EventId = 60866, Level = LogLevel.Error, Message = "{procedure} answered no state for run {executionId}, so the run is not assumed claimed")]
    public static partial IGenericMessage ProcedureAnsweredNoState(ILogger logger, string procedure, Guid executionId);

    /// <summary>Logs a procedure that answered a state the engine does not know.</summary>
    [MessageLogging(EventId = 60867, Level = LogLevel.Error, Message = "{procedure} answered the state '{state}' for run {executionId}: only Claimed and Waiting are answered, so the run is not assumed claimed")]
    public static partial IGenericMessage ProcedureAnsweredUnknownState(ILogger logger, string procedure, Guid executionId, string state);

    /// <summary>Logs a lease that has passed before the next step.</summary>
    [MessageLogging(EventId = 60868, Level = LogLevel.Error,
        Message = "The claim's lease of {lease} has passed without a renewal, so the run may no longer hold its claim and stops before its next step")]
    public static partial IGenericMessage LeaseLapsed(ILogger logger, TimeSpan lease);

    /// <summary>Logs an answer of a procedure that cannot be read.</summary>
    [MessageLogging(EventId = 60869, Level = LogLevel.Error, Message = "{procedure} answered a row whose column {column} cannot be read: {reason}")]
    public static partial IGenericMessage AnswerNotReadable(ILogger logger, string procedure, string column, string reason);

    /// <summary>A host that runs pipelines registers no settings service, so the four settings of the durable claim cannot be read when it starts (FDW-6939).</summary>
    [MessageLogging(EventId = 60870, Level = LogLevel.Error, Message = "The host runs pipelines and registers no settings service, so the settings of the durable claim of runs cannot be read and the host does not start")]
    public static partial IGenericMessage SettingsNotRegistered(ILogger logger);

    /// <summary>A run with no tenant cannot be claimed: the claim rows state the tenant, and none is assumed for it (FDW-6939).</summary>
    [MessageLogging(EventId = 60871, Level = LogLevel.Error, Message = "Run {executionId} states no tenant, so its claim cannot be made and none is assumed for it")]
    public static partial IGenericMessage RunHasNoTenant(ILogger logger, Guid executionId);

    /// <summary>A placeholder of a pipeline's claim key template names a parameter the run did not state, or states with no text; nothing completes it (FDW-6939).</summary>
    [MessageLogging(EventId = 60872, Level = LogLevel.Error, Message = "The claim key of the pipeline names the parameter {parameter}, and the run states no text for it, so the key is not made and none is assumed for it")]
    public static partial IGenericMessage TemplateParameterHasNoValue(ILogger logger, string parameter);

    /// <summary>A claim key template has a placeholder that is not closed or names nothing (FDW-6939).</summary>
    [MessageLogging(EventId = 60873, Level = LogLevel.Error, Message = "The claim key template '{template}' has a placeholder that is not closed or names no parameter")]
    public static partial IGenericMessage TemplateMalformed(ILogger logger, string template);

    /// <summary>A place on a key that is held: the hold stands and the refusal names who holds it (FDW-6939).</summary>
    [MessageLogging(EventId = 40874, Level = LogLevel.Error, Message = "The key {key} is already held by {heldBy} since {heldAt}, who gave the reason: {reason}; the hold stands and a second one is not placed")]
    public static partial IGenericMessage KeyAlreadyHeld(ILogger logger, string key, Guid heldBy, string heldAt, string reason);

    /// <summary>A release of a key that is not held (FDW-6939).</summary>
    [MessageLogging(EventId = 30875, Level = LogLevel.Error, Message = "The key {key} is not held, so there is no hold to release")]
    public static partial IGenericMessage KeyNotHeld(ILogger logger, string key);

    /// <summary>A hold with no reason: the reason is stated, never assumed (FDW-6939).</summary>
    [MessageLogging(EventId = 20876, Level = LogLevel.Error, Message = "The hold on the key {key} states no reason, so it is not placed and none is assumed for it")]
    public static partial IGenericMessage HoldReasonMissing(ILogger logger, string key);

    /// <summary>The renewal of a run's claim ended because the hold was disposed or handed back (FDW-6939).</summary>
    [MessageLogging(EventId = 60877, Level = LogLevel.Debug, Message = "The renewal of the claim of run {executionId} has ended")]
    public static partial IGenericMessage RenewalEnded(ILogger logger, Guid executionId, Exception exception);

    /// <summary>A hold placed or released by a caller that is no person (not signed in, or a system or tenant-run context): who holds a key is a fact about a person (FDW-6939).</summary>
    [MessageLogging(EventId = 50878, Level = LogLevel.Error, Message = "The caller of the hold on a key is not a signed-in person, so the hold is not placed or released")]
    public static partial IGenericMessage HoldCallerIsNoPerson(ILogger logger);

    /// <summary>A hold placed or released by a caller that acts in no tenant: a hold is for the tenant of the caller and none is assumed (FDW-6939).</summary>
    [MessageLogging(EventId = 20879, Level = LogLevel.Error, Message = "The caller of the hold on a key acts in no tenant, so the hold is not placed or released and no tenant is assumed")]
    public static partial IGenericMessage HoldCallerHasNoTenant(ILogger logger);
}
