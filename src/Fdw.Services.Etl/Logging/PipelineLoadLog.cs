using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the structural validation of a load row (FDW-1108): columns, versioning, soft delete and value sources.
/// Every refusal is logged and returned in the failure result.
/// EventId range: 20001-20099.
/// </summary>
[MessageLoggingTypeCode("PIPELINELOAD")]
public static partial class PipelineLoadLog
{
    /// <summary>Logs a refused load configuration: In owner: option is needed and not stated.</summary>
    [MessageLogging(EventId = 20015, Level = LogLevel.Warning,
        Message = "In {owner}: {option} is needed and not stated")]
    public static partial IGenericMessage LoadStepOptionMissing(ILogger logger, string owner, string option);

    /// <summary>Logs a refused load configuration: In owner: option is stated, which kind does not use.</summary>
    [MessageLogging(EventId = 20016, Level = LogLevel.Warning,
        Message = "In {owner}: {option} is stated, which {kind} does not use")]
    public static partial IGenericMessage LoadStepOptionNotApplicable(ILogger logger, string owner, string option, string kind);

    /// <summary>Logs a refused load configuration: In owner: a column has role 'role', which is not a load column role.</summary>
    [MessageLogging(EventId = 20017, Level = LogLevel.Warning,
        Message = "In {owner}: a column has role '{role}', which is not a load column role")]
    public static partial IGenericMessage LoadColumnRoleUnknown(ILogger logger, string owner, string role);

    /// <summary>Logs a refused load configuration: In owner: a column has role 'role', which is not valid there: expected.</summary>
    [MessageLogging(EventId = 20018, Level = LogLevel.Warning,
        Message = "In {owner}: a column has role '{role}', which is not valid there: {expected}")]
    public static partial IGenericMessage LoadColumnRoleNotForOwner(ILogger logger, string owner, string role, string expected);

    /// <summary>Logs a refused load configuration: In owner: two role columns are at ordinal ordinal.</summary>
    [MessageLogging(EventId = 20019, Level = LogLevel.Warning,
        Message = "In {owner}: two {role} columns are at ordinal {ordinal}")]
    public static partial IGenericMessage LoadColumnOrdinalDuplicated(ILogger logger, string owner, string role, int ordinal);

    /// <summary>Logs a refused load configuration: In owner: a role column has no column name.</summary>
    [MessageLogging(EventId = 20020, Level = LogLevel.Warning,
        Message = "In {owner}: a {role} column has no column name")]
    public static partial IGenericMessage LoadColumnNameBlank(ILogger logger, string owner, string role);

    /// <summary>Logs a refused load configuration: In owner: option is stated but not missing; both are needed.</summary>
    [MessageLogging(EventId = 20021, Level = LogLevel.Warning,
        Message = "In {owner}: {option} is stated but not {missing}; both are needed")]
    public static partial IGenericMessage LoadOptionPairIncomplete(ILogger logger, string owner, string option, string missing);

    /// <summary>Logs a refused load configuration: In owner: soft delete mode 'mode' is not a soft delete mode.</summary>
    [MessageLogging(EventId = 20022, Level = LogLevel.Warning,
        Message = "In {owner}: soft delete mode '{mode}' is not a soft delete mode")]
    public static partial IGenericMessage SoftDeleteModeUnknown(ILogger logger, string owner, string mode);

    /// <summary>Logs a refused load configuration: In owner: the soft delete options do not fit together: reason.</summary>
    [MessageLogging(EventId = 20023, Level = LogLevel.Warning,
        Message = "In {owner}: the soft delete options do not fit together: {reason}")]
    public static partial IGenericMessage SoftDeleteOptionsInconsistent(ILogger logger, string owner, string reason);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' has kind 'kind', which is not a value source kind.</summary>
    [MessageLogging(EventId = 20024, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' has kind '{kind}', which is not a value source kind")]
    public static partial IGenericMessage ValueSourceKindUnknown(ILogger logger, string owner, string valueSource, string kind);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' needs option.</summary>
    [MessageLogging(EventId = 20025, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' needs {option}")]
    public static partial IGenericMessage ValueSourceOptionMissing(ILogger logger, string owner, string valueSource, string option);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' states option, which a kind value source does not use.</summary>
    [MessageLogging(EventId = 20026, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' states {option}, which a {kind} value source does not use")]
    public static partial IGenericMessage ValueSourceOptionNotApplicable(ILogger logger, string owner, string valueSource, string option, string kind);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' is a kind value source, which maps a source column and only a load that takes a source data set has one.</summary>
    [MessageLogging(EventId = 20027, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' is a {kind} value source, which maps a source column and only a load that takes a source data set has one")]
    public static partial IGenericMessage ValueSourceNeedsSourceDataSet(ILogger logger, string owner, string valueSource, string kind);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' is a kind value source, which cannot supply the parameter of an executable container.</summary>
    [MessageLogging(EventId = 20028, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' is a {kind} value source, which cannot supply the parameter of an executable container")]
    public static partial IGenericMessage ValueSourceNotForContainer(ILogger logger, string owner, string valueSource, string kind);

    /// <summary>Logs a refused load configuration: In owner: value source 'valueSource' reads its constant as type 'parameterType', which is not a data type.</summary>
    [MessageLogging(EventId = 20030, Level = LogLevel.Warning,
        Message = "In {owner}: value source '{valueSource}' reads its constant as type '{parameterType}', which is not a data type")]
    public static partial IGenericMessage ValueSourceConstantTypeUnknown(ILogger logger, string owner, string valueSource, string parameterType);

    /// <summary>Logs a refused load configuration: In owner: a value is supplied for 'columnName' by two value sources.</summary>
    [MessageLogging(EventId = 20031, Level = LogLevel.Warning,
        Message = "In {owner}: a value is supplied for '{columnName}' by two value sources")]
    public static partial IGenericMessage ValueSourceColumnDuplicated(ILogger logger, string owner, string columnName);

}
