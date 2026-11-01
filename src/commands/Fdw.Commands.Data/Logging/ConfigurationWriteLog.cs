using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for the data commands a configuration save or retire expands into: what it writes, what it matches on,
/// and why it refuses.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class ConfigurationWriteLog
{
    /// <summary>Traces the plan of a configuration save: what it replaces, writes and returns.</summary>
    [MessageLogging(EventId = 12070, Level = LogLevel.Trace,
        Message = "[ConfigurationSave] '{container}' retires the current row matching [{logicalFields}], writes [{columns}], returns [{returnedFields}]")]
    public static partial IGenericMessage SavePlanned(ILogger logger, string container, string logicalFields, string columns, string returnedFields);

    /// <summary>Traces the plan of a configuration retire: what it matches and returns.</summary>
    [MessageLogging(EventId = 12041, Level = LogLevel.Trace,
        Message = "[ConfigurationRetire] '{container}' retires the current rows matching [{matchFields}] (markDeleted={markDeleted}), returns [{returnedFields}]")]
    public static partial IGenericMessage RetirePlanned(ILogger logger, string container, string matchFields, bool markDeleted, string returnedFields);

    /// <summary>Logs a configuration save with no mapper for its record type.</summary>
    [MessageLogging(EventId = 62004, Level = LogLevel.Critical,
        Message = "[ConfigurationSave] no PocoMapper registered for type '{typeName}'; ensure it has [GenerateMapper]")]
    public static partial IGenericMessage NoPocoMapperRegistered(ILogger logger, string typeName);

    /// <summary>Logs a configuration save that has no column to write.</summary>
    [MessageLogging(EventId = 62005, Level = LogLevel.Critical,
        Message = "[ConfigurationSave] no insertable columns for type '{typeName}' in container '{container}'")]
    public static partial IGenericMessage NoInsertableColumns(ILogger logger, string typeName, string container);

    /// <summary>Logs a configuration write against a container that is not a keyed data container.</summary>
    [MessageLogging(EventId = 62006, Level = LogLevel.Error,
        Message = "[{commandType}] container '{container}' is not a keyed data container, so no row of it can be versioned")]
    public static partial IGenericMessage ContainerHasNoKeys(ILogger logger, string commandType, string container);

    /// <summary>Logs a retire that names a key the container does not declare.</summary>
    [MessageLogging(EventId = 62010, Level = LogLevel.Error,
        Message = "[ConfigurationRetire] container '{container}' declares no {keyType} key to retire by")]
    public static partial IGenericMessage KeyNotDeclared(ILogger logger, string container, string keyType);

    /// <summary>Logs a foreign key whose referenced field is not resolved in the configuration.</summary>
    [MessageLogging(EventId = 62012, Level = LogLevel.Error,
        Message = "[{commandType}] foreign key '{keyName}' on '{referencingContainer}' references '{container}' but its referenced field is not resolved in the configuration")]
    public static partial IGenericMessage ReferencedFieldNotResolved(ILogger logger, string commandType, string keyName, string referencingContainer, string container);

    /// <summary>Logs a save that carries nothing to say which current row it replaces.</summary>
    [MessageLogging(EventId = 62013, Level = LogLevel.Error,
        Message = "[ConfigurationSave] save to '{container}' carries no owner key values and the container declares no Logical key, so nothing says which current row it replaces")]
    public static partial IGenericMessage LogicalKeyNotDeclared(ILogger logger, string container);

    /// <summary>Logs a save with no value for a field of its Logical key.</summary>
    [MessageLogging(EventId = 62014, Level = LogLevel.Error,
        Message = "[ConfigurationSave] save to '{container}' has no value for field '{field}' of its Logical key '{keyName}'")]
    public static partial IGenericMessage LogicalKeyValueMissing(ILogger logger, string container, string keyName, string field);

    /// <summary>Logs a configuration write against a container that declares no fields.</summary>
    [MessageLogging(EventId = 62015, Level = LogLevel.Error,
        Message = "[{commandType}] container '{container}' declares no fields, so the columns it writes are unknown")]
    public static partial IGenericMessage ContainerFieldsNotDeclared(ILogger logger, string commandType, string container);

    /// <summary>Logs a retire whose key has a different number of fields than values.</summary>
    [MessageLogging(EventId = 62016, Level = LogLevel.Error,
        Message = "[ConfigurationRetire] container '{container}' {keyType} key has {fieldCount} field(s) but the retire carries {valueCount} value(s)")]
    public static partial IGenericMessage KeyValueCountMismatch(ILogger logger, string container, string keyType, int fieldCount, int valueCount);

    /// <summary>Logs a retire that matches on a field the container does not declare.</summary>
    [MessageLogging(EventId = 62017, Level = LogLevel.Error,
        Message = "[ConfigurationRetire] container '{container}' declares no field '{field}' to retire by")]
    public static partial IGenericMessage MatchFieldNotDeclared(ILogger logger, string container, string field);

    /// <summary>Logs a retire that matches on nothing, which would retire every current row.</summary>
    [MessageLogging(EventId = 62018, Level = LogLevel.Error,
        Message = "[ConfigurationRetire] retire of '{container}' matches on no field; refusing to retire every current row")]
    public static partial IGenericMessage NothingToMatch(ILogger logger, string container);

    /// <summary>Logs a configuration write that matches on a field it has no value for.</summary>
    [MessageLogging(EventId = 62019, Level = LogLevel.Error,
        Message = "[{commandType}] write to '{container}' has no value for field '{field}', which it matches the current row on")]
    public static partial IGenericMessage MatchValueMissing(ILogger logger, string commandType, string container, string field);

    /// <summary>Logs a configuration write against a container that does not declare the columns a version is written with.</summary>
    [MessageLogging(EventId = 62022, Level = LogLevel.Error,
        Message = "[{commandType}] container '{container}' does not declare the version column '{column}', so a version of a row cannot be written")]
    public static partial IGenericMessage VersionColumnNotDeclared(ILogger logger, string commandType, string container, string column);

    /// <summary>Logs a configuration save whose insert answered no row, so the written key is unknown.</summary>
    [MessageLogging(EventId = 62020, Level = LogLevel.Error,
        Message = "[ConfigurationSave] the insert answered no row, so the key of the row written is unknown")]
    public static partial IGenericMessage InsertAnsweredNoRow(ILogger logger);

    /// <summary>Logs a configuration write whose steps answered differently from what it expanded into.</summary>
    [MessageLogging(EventId = 62021, Level = LogLevel.Error,
        Message = "[{commandType}] its {stepCount} step(s) did not answer the rows it needs to assemble its result")]
    public static partial IGenericMessage AnswersNotAssemblable(ILogger logger, string commandType, int stepCount);
}
