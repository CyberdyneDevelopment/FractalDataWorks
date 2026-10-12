using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Quality.Logging;

/// <summary>
/// MessageLogging methods for Quality operations.
/// Every log message is returned in the result AND logged.
/// EventId range: 8300-8349
/// </summary>
[MessageLoggingTypeCode("QUALITY")]
public static partial class QualityLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Rule Management Events (8300-8309)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs creation of a quality rule.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information,
        Message = "Quality rule '{ruleName}' created for DataSet '{dataSetName}'")]
    public static partial IGenericMessage RuleCreated(ILogger logger, string ruleName, string dataSetName);

    /// <summary>Logs update of a quality rule.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information,
        Message = "Quality rule '{ruleName}' updated")]
    public static partial IGenericMessage RuleUpdated(ILogger logger, string ruleName);

    /// <summary>Logs deletion of a quality rule.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information,
        Message = "Quality rule '{ruleName}' deleted")]
    public static partial IGenericMessage RuleDeleted(ILogger logger, string ruleName);

    /// <summary>Logs loading quality rules for a data set.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Debug,
        Message = "Loading quality rules for DataSet '{dataSetName}'")]
    public static partial IGenericMessage LoadingRules(ILogger logger, string dataSetName);

    /// <summary>Logs the count of quality rules loaded.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information,
        Message = "Loaded {ruleCount} quality rules for DataSet '{dataSetName}'")]
    public static partial IGenericMessage RulesLoaded(ILogger logger, int ruleCount, string dataSetName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Quality Check Execution Events (8310-8319)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of a quality check.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Information,
        Message = "Quality check started for DataSet '{dataSetName}' ({ruleCount} rules)")]
    public static partial IGenericMessage CheckStarted(ILogger logger, string dataSetName, int ruleCount);

    /// <summary>Logs completion of a quality check.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Information,
        Message = "Quality check completed for DataSet '{dataSetName}': {passedCount}/{totalCount} rules passed")]
    public static partial IGenericMessage CheckCompleted(ILogger logger, string dataSetName, int passedCount, int totalCount);

    /// <summary>Logs execution of a specific quality rule.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Debug,
        Message = "Executing rule '{ruleName}' (type: {ruleType})")]
    public static partial IGenericMessage ExecutingRule(ILogger logger, string ruleName, string ruleType);

    /// <summary>Logs a quality rule that passed.</summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Information,
        Message = "Rule '{ruleName}' passed: {passedRecords}/{totalRecords} records ({passRate:P2})")]
    public static partial IGenericMessage RulePassed(ILogger logger, string ruleName, int passedRecords, int totalRecords, double passRate);

    /// <summary>Logs a quality rule that failed.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Warning,
        Message = "Rule '{ruleName}' failed: {failedRecords} violations found")]
    public static partial IGenericMessage RuleFailed(ILogger logger, string ruleName, int failedRecords);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Events (8340-8349)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a quality rule was not found.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Error,
        Message = "Quality rule not found: '{ruleId}'")]
    public static partial IGenericMessage RuleNotFound(ILogger logger, Guid ruleId);

    /// <summary>Logs an invalid rule type.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "Invalid rule type: '{ruleType}'")]
    public static partial IGenericMessage InvalidRuleType(ILogger logger, string ruleType);

    /// <summary>Logs a failed quality check.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "Quality check failed for DataSet '{dataSetName}'")]
    public static partial IGenericMessage CheckFailed(ILogger logger, Exception exception, string dataSetName);

    /// <summary>Logs a failed rule execution.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "Rule execution failed for '{ruleName}': {error}")]
    public static partial IGenericMessage RuleExecutionFailed(ILogger logger, string ruleName, string error);

    /// <summary>Logs a failure to save a quality rule.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to save quality rule '{ruleName}'")]
    public static partial IGenericMessage RuleSaveFailed(ILogger logger, Exception exception, string ruleName);
}
