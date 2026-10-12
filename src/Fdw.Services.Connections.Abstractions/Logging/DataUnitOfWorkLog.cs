using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="UnitOfWork.DataUnitOfWork"/>.
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS6")]
public static partial class DataUnitOfWorkLog
{
    /// <summary>Traces a unit of work starting.</summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Trace,
        Message = "[DataUnitOfWork] Running {stepCount} steps in one transaction")]
    public static partial IGenericMessage Starting(ILogger logger, int stepCount);

    /// <summary>Records a unit of work that committed.</summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Information,
        Message = "[DataUnitOfWork] Committed {stepCount} steps, {rowCount} rows affected")]
    public static partial IGenericMessage Committed(ILogger logger, int stepCount, int rowCount);

    /// <summary>Records a unit of work whose transaction could not be started.</summary>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "[DataUnitOfWork] The transaction could not be started, nothing ran")]
    public static partial IGenericMessage BeginFailed(ILogger logger);

    /// <summary>Records a step that failed, so the whole unit of work was rolled back.</summary>
    [MessageLogging(EventId = 71013, Level = LogLevel.Warning,
        Message = "[DataUnitOfWork] Step {stepNumber} of {stepCount} ('{commandType}') failed, rolling back the {rolledBack} steps before it")]
    public static partial IGenericMessage StepFailed(ILogger logger, int stepNumber, int stepCount, string commandType, int rolledBack);

    /// <summary>Records a commit that failed, so no step took effect.</summary>
    [MessageLogging(EventId = 71014, Level = LogLevel.Error,
        Message = "[DataUnitOfWork] The commit failed after {stepCount} steps ran")]
    public static partial IGenericMessage CommitFailed(ILogger logger, int stepCount);

    /// <summary>Records a rollback that itself failed.</summary>
    [MessageLogging(EventId = 71015, Level = LogLevel.Error,
        Message = "[DataUnitOfWork] The rollback failed after step {stepNumber} failed")]
    public static partial IGenericMessage RollbackFailed(ILogger logger, int stepNumber);
}
