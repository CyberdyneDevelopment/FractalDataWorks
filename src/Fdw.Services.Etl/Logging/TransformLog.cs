using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the Sort, LatestBy, Distinct (FDW-1109) and Validate (FDW-1120) transforms: a refused step configuration, a record that
/// lacks a field the step reads, values that cannot be ordered, and what each step did. Every refusal is logged and
/// returned in the failure result.
/// EventId range: 20101-20199.
/// </summary>
[MessageLoggingTypeCode("TRANSFORM")]
public static partial class TransformLog
{
    /// <summary>Logs a refused transform step: its configuration is unusable.</summary>
    [MessageLogging(EventId = 20101, Level = LogLevel.Error,
        Message = "Transform '{name}' ({transformType}) is not usable: {reason}")]
    public static partial IGenericMessage TransformParamsInvalid(ILogger logger, string name, string transformType, string reason);

    /// <summary>Logs a record that does not carry a field the transform reads.</summary>
    [MessageLogging(EventId = 20102, Level = LogLevel.Error,
        Message = "Transform '{name}' ({transformType}) read a record without the field '{fieldName}'")]
    public static partial IGenericMessage RecordFieldMissing(ILogger logger, string name, string transformType, string fieldName);

    /// <summary>Logs values of one field that cannot be ordered against each other.</summary>
    [MessageLogging(EventId = 20103, Level = LogLevel.Error,
        Message = "Transform '{name}' ({transformType}) cannot order the values of field '{fieldName}': {leftType} against {rightType}")]
    public static partial IGenericMessage ValuesNotComparable(ILogger logger, string name, string transformType, string fieldName, string leftType, string rightType);

    /// <summary>Logs that a Sort step ordered its records.</summary>
    [MessageLogging(EventId = 20104, Level = LogLevel.Debug,
        Message = "Sort '{name}' ordered {count} records by [{sortFields}]")]
    public static partial IGenericMessage SortApplied(ILogger logger, string name, int count, string sortFields);

    /// <summary>Logs that a LatestBy step kept one record per key.</summary>
    [MessageLogging(EventId = 20105, Level = LogLevel.Debug,
        Message = "LatestBy '{name}' kept {outCount} of {inCount} records, one per key [{keyFields}], by {orderField} ({keepRule})")]
    public static partial IGenericMessage LatestByApplied(ILogger logger, string name, int inCount, int outCount, string keyFields, string orderField, string keepRule);

    /// <summary>Logs that a Distinct step kept the first record of each key.</summary>
    [MessageLogging(EventId = 20106, Level = LogLevel.Debug,
        Message = "Distinct '{name}' kept {outCount} of {inCount} records, one per key [{keyFields}]")]
    public static partial IGenericMessage DistinctApplied(ILogger logger, string name, int inCount, int outCount, string keyFields);

    /// <summary>Logs that a Validate step checked its records and how many it rejected.</summary>
    [MessageLogging(EventId = 20107, Level = LogLevel.Debug,
        Message = "Validate '{name}' checked {inCount} records and rejected {rejectedCount}")]
    public static partial IGenericMessage ValidateApplied(ILogger logger, string name, int inCount, int rejectedCount);

    /// <summary>Logs what a Branch stage did: how many records each route took.</summary>
    [MessageLogging(EventId = 20150, Level = LogLevel.Debug,
        Message = "Branch '{name}' routed {inCount} record(s): {pathCounts}; {unmatchedCount} matched no path")]
    public static partial IGenericMessage BranchApplied(ILogger logger, string name, int inCount, string pathCounts, int unmatchedCount);
}
