using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Logging;

/// <summary>
/// MessageLogging for the reading of a graph's SQL steps (FDW-5457): the refusals of a save whose script does not parse or names what its data
/// store does not hold, the warnings of what it cannot judge, and what the lineage does with a stored script it cannot read. Every refusal and
/// warning is logged and returned; a warning does not stop a save.
/// EventId range: 21440-21449.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class GraphScriptLog
{
    /// <summary>Logs a script that does not parse.</summary>
    [MessageLogging(EventId = 21440, Level = LogLevel.Error,
        Message = "The script of component '{component}' on data store '{dataStore}' does not parse: {errors}")]
    public static partial IGenericMessage ScriptNotParsed(ILogger logger, string component, string dataStore, string errors);

    /// <summary>Logs a script that names a table its data store does not hold.</summary>
    [MessageLogging(EventId = 21441, Level = LogLevel.Error,
        Message = "The script of component '{component}' names '{name}', which data store '{dataStore}' does not hold")]
    public static partial IGenericMessage ScriptObjectMissing(ILogger logger, string component, string name, string dataStore);

    /// <summary>Logs a script that names a table with no path, which more than one path of its data store holds.</summary>
    [MessageLogging(EventId = 21442, Level = LogLevel.Error,
        Message = "The script of component '{component}' names '{name}' with no schema, and data store '{dataStore}' holds one in each of {paths}; name the schema")]
    public static partial IGenericMessage ScriptObjectAmbiguous(ILogger logger, string component, string name, string dataStore, string paths);

    /// <summary>Logs a script whose data store cannot be read.</summary>
    [MessageLogging(EventId = 21443, Level = LogLevel.Error,
        Message = "The script of component '{component}' runs on data store '{dataStore}', which cannot be read: {reason}")]
    public static partial IGenericMessage ScriptStoreNotRead(ILogger logger, string component, string dataStore, string reason);

    /// <summary>Warns of a script on a data store whose kind has no script reader on this host: it is saved, not checked, and draws no lineage.</summary>
    [MessageLogging(EventId = 21444, Level = LogLevel.Warning,
        Message = "The script of component '{component}' runs on data store '{dataStore}' of kind '{kind}', whose scripts this host cannot read; its tables are not checked and draw no lineage")]
    public static partial IGenericMessage ScriptLanguageNotRead(ILogger logger, string component, string dataStore, string kind);

    /// <summary>Warns of a script that names objects in another database or server: they are legal references the store's catalogue cannot hold, so they are not checked and draw no lineage.</summary>
    [MessageLogging(EventId = 21445, Level = LogLevel.Warning,
        Message = "The script of component '{component}' names {names}, which are outside data store '{dataStore}'; they are not checked and draw no lineage")]
    public static partial IGenericMessage ScriptNamesOutsideStore(ILogger logger, string component, string names, string dataStore);

    /// <summary>Logs a stored script the lineage cannot read: it draws no line, and the pipeline is read as it is.</summary>
    [MessageLogging(EventId = 21446, Level = LogLevel.Warning,
        Message = "Pipeline '{pipeline}' component '{component}': its script draws no lineage because {reason}")]
    public static partial IGenericMessage ScriptNotDrawn(ILogger logger, string pipeline, string component, string? reason);

    /// <summary>Logs the names of a script that are given at run time, which nothing judges before the run.</summary>
    [MessageLogging(EventId = 21447, Level = LogLevel.Debug,
        Message = "The script of component '{component}' on data store '{dataStore}' names {names} with a part given at run time; they are judged when it runs")]
    public static partial IGenericMessage ScriptNamesAtRunTime(ILogger logger, string component, string dataStore, string names);

    /// <summary>Warns of a script on a data store that has no catalogued containers: its tables cannot be checked until the store is discovered.</summary>
    [MessageLogging(EventId = 21449, Level = LogLevel.Warning,
        Message = "Data store '{dataStore}' has no catalogue (it has not been discovered), so the tables of the script of component '{component}' are not checked")]
    public static partial IGenericMessage ScriptStoreNotCatalogued(ILogger logger, string dataStore, string component);

    /// <summary>Logs a script whose data store is a parameter, so its names are judged when it runs.</summary>
    [MessageLogging(EventId = 21448, Level = LogLevel.Debug,
        Message = "The script of component '{component}' runs on the data store '{dataStore}' given at run time; its names are judged when it runs")]
    public static partial IGenericMessage ScriptStoreAtRunTime(ILogger logger, string component, string dataStore);
}
