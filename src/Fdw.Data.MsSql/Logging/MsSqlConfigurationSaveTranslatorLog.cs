using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlConfigurationSaveTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlConfigurationSaveTranslatorLog
{
    [MessageLogging(
        EventId = 12050,
        Level = LogLevel.Trace,
        Message = "MsSqlConfigurationSaveTranslator translating ConfigurationSaveCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12051,
        Level = LogLevel.Information,
        Message = "MsSqlConfigurationSaveTranslator built version-on-write save for container '{container}' with {columnCount} column(s)")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container,
        int columnCount);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: {commandType} for container '{container}' has no input data")]
    public static partial IGenericMessage MissingInputData(
        ILogger logger,
        string commandType,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the container's database path declares no schema.
    /// </summary>
    [MessageLogging(
        EventId = 62002,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationSaveTranslator: container '{container}' has no schema defined; ConfigurationSave requires a schema-qualified table path")]
    public static partial IGenericMessage NoSchemaDefined(
        ILogger logger,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the input data's CLR type has no source-generated
    /// PocoMapper registered ([GenerateMapper] missing).
    /// </summary>
    [MessageLogging(
        EventId = 62004,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationSaveTranslator: no PocoMapper registered for type '{typeName}'; ensure it has [GenerateMapper]")]
    public static partial IGenericMessage NoPocoMapperRegistered(
        ILogger logger,
        string typeName);

    /// <summary>
    /// Logs the unrecoverable state where the mapper/container-field intersection yields no
    /// insertable columns and no resolvable foreign keys.
    /// </summary>
    [MessageLogging(
        EventId = 62005,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationSaveTranslator: no insertable columns for type '{typeName}' in container '{container}'")]
    public static partial IGenericMessage NoInsertableColumns(
        ILogger logger,
        string typeName,
        string container);

    /// <summary>Logged when the container a save targets carries no declared keys to address its rows by.</summary>
    [MessageLogging(
        EventId = 62006,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: container '{container}' is not a keyed data container, so no row of it can be versioned")]
    public static partial IGenericMessage ContainerHasNoKeys(
        ILogger logger,
        string container);

    /// <summary>
    /// Logged when this translator is handed a command that is not a ConfigurationSave, so it carries
    /// no stated owner key type.
    /// </summary>
    [MessageLogging(
        EventId = 62007,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: command type '{commandType}' is not a ConfigurationSaveCommand")]
    public static partial IGenericMessage NotAConfigurationSaveCommand(
        ILogger logger,
        string commandType);

    /// <summary>
    /// Logged when a Foreign key referencing the saved container has a key field whose referenced
    /// field was not resolved, so the value its children need cannot be returned.
    /// </summary>
    [MessageLogging(
        EventId = 62012,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: foreign key '{keyName}' on '{referencingContainer}' references '{container}' but its referenced field is not resolved in the configuration")]
    public static partial IGenericMessage ReferencedFieldNotResolved(
        ILogger logger,
        string keyName,
        string referencingContainer,
        string container);

    /// <summary>Logged when a save has neither owner key values nor a declared Logical key to match the current row on.</summary>
    [MessageLogging(
        EventId = 62013,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: save to '{container}' carries no owner key values and the container declares no Logical key, so nothing says which current row it replaces")]
    public static partial IGenericMessage LogicalKeyNotDeclared(
        ILogger logger,
        string container);

    /// <summary>Logged when a save carries no value for a field of the container's Logical key.</summary>
    [MessageLogging(
        EventId = 62014,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: save to '{container}' has no value for field '{field}' of its Logical key '{keyName}'")]
    public static partial IGenericMessage LogicalKeyValueMissing(
        ILogger logger,
        string container,
        string keyName,
        string field);

    /// <summary>Logged when the saved container declares no fields, so nothing says which columns a save may write.</summary>
    [MessageLogging(
        EventId = 62015,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: container '{container}' declares no fields, so the columns a save writes are unknown")]
    public static partial IGenericMessage ContainerFieldsNotDeclared(
        ILogger logger,
        string container);

    /// <summary>Traces the save a translation settled on: what it matches, writes and returns.</summary>
    [MessageLogging(
        EventId = 12070,
        Level = LogLevel.Trace,
        Message = "MsSqlConfigurationSaveTranslator: '{container}' retires the current row matching [{logicalFields}], writes [{columns}], returns [{returnedFields}]")]
    public static partial IGenericMessage SavePlanned(
        ILogger logger,
        string container,
        string logicalFields,
        string columns,
        string returnedFields);

    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator failed to translate save for container '{container}': {errorMessage}")]
    public static partial IGenericMessage SaveTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
