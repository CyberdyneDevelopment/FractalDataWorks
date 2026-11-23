using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the slot file (FDW-5845): the file <c>APP_SLOT_FILE</c> names, which a host running on an A/B slot
/// reads beside the environment to resolve the <c>${NAME}</c> placeholders of <c>configurationSchema.json</c>.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class SlotFileLog
{
    /// <summary>The slot file cannot be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="reason">Why the read failed.</param>
    [MessageLogging(EventId = 61110, Level = LogLevel.Error,
        Message = "The slot file '{filePath}' named by APP_SLOT_FILE cannot be read: {reason}. A host that runs on a slot reads its ports, addresses and database names from that file, and nothing stands in for it")]
    public static partial IGenericMessage Unreadable(ILogger logger, string filePath, string reason);

    /// <summary>The slot file is not a JSON object.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="reason">What the parser reported.</param>
    [MessageLogging(EventId = 61111, Level = LogLevel.Error,
        Message = "The slot file '{filePath}' named by APP_SLOT_FILE is not a JSON object: {reason}")]
    public static partial IGenericMessage NotAJsonObject(ILogger logger, string filePath, string reason);

    /// <summary>Two entries of the slot file flatten to the same name.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="name">The name that two entries produce.</param>
    [MessageLogging(EventId = 61112, Level = LogLevel.Error,
        Message = "The slot file '{filePath}' states the name '{name}' twice (a path of nested keys joined with '__' gives the same name as another entry), so a placeholder of that name would have two values")]
    public static partial IGenericMessage NameStatedTwice(ILogger logger, string filePath, string name);

    /// <summary>A placeholder's name is stated by the environment and by the slot file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="schemaPath">The schema file holding the placeholders.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="names">The names stated both ways.</param>
    [MessageLogging(EventId = 61113, Level = LogLevel.Error,
        Message = "configurationSchema.json at '{schemaPath}' has placeholder(s) {names} that are stated both by an environment variable and by the slot file '{filePath}'. A value has one source: remove one of the two")]
    public static partial IGenericMessage PlaceholderStatedByBothSources(ILogger logger, string schemaPath, string filePath, string names);

    /// <summary>A host that runs on a slot has a placeholder that neither the environment nor its slot file states.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="schemaPath">The schema file holding the placeholders.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="names">The names neither source states.</param>
    [MessageLogging(EventId = 61114, Level = LogLevel.Error,
        Message = "configurationSchema.json at '{schemaPath}' references {names}, which neither an environment variable nor the slot file '{filePath}' states. Name each by its path in the slot file with '__' between keys (databases__LogicalName), or set the environment variable")]
    public static partial IGenericMessage PlaceholderNotStatedOnSlot(ILogger logger, string schemaPath, string filePath, string names);

    /// <summary>A host that does not run on a slot has a placeholder whose environment variable is unset.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="schemaPath">The schema file holding the placeholders.</param>
    /// <param name="names">The unset variables.</param>
    [MessageLogging(EventId = 61115, Level = LogLevel.Error,
        Message = "configurationSchema.json at '{schemaPath}' references environment variable(s) that are not set: {names}. Set them in the host environment before starting the application")]
    public static partial IGenericMessage PlaceholderNotStated(ILogger logger, string schemaPath, string names);

    /// <summary>A row's text states a placeholder that the environment and the slot file both state (FDW-5847).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="owner">What states the text.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="keys">The keys stated both ways.</param>
    [MessageLogging(EventId = 61159, Level = LogLevel.Error,
        Message = "{owner} states placeholder(s) {keys} that are stated both by an environment variable and by the slot file '{filePath}'. A value has one source: remove one of the two")]
    public static partial IGenericMessage RowPlaceholderStatedByBothSources(ILogger logger, string owner, string filePath, string keys);

    /// <summary>A row's text states a placeholder that neither the environment nor the slot file states (FDW-5847).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="owner">What states the text.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="keys">The keys neither source states.</param>
    [MessageLogging(EventId = 61160, Level = LogLevel.Error,
        Message = "{owner} states placeholder(s) {keys}, which neither an environment variable nor the slot file '{filePath}' states. Write each as its path in the slot file with '__' between keys (hostnames__live__0), or set the environment variable")]
    public static partial IGenericMessage RowPlaceholderNotStatedOnSlot(ILogger logger, string owner, string filePath, string keys);

    /// <summary>A row's text states a placeholder on a host that is not on a slot and has no such environment variable (FDW-5847).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="owner">What states the text.</param>
    /// <param name="keys">The keys with no environment variable.</param>
    [MessageLogging(EventId = 61161, Level = LogLevel.Error,
        Message = "{owner} states placeholder(s) {keys}, and this host is not on a slot (APP_SLOT_FILE is not set) and has no environment variable of that spelling. Set the variable, or write the value itself in the row")]
    public static partial IGenericMessage RowPlaceholderNotStated(ILogger logger, string owner, string keys);

    /// <summary>A row's text states placeholders, and the slot file that would fill them cannot be read (FDW-5847).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="owner">What states the text.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="keys">The keys left unfilled.</param>
    [MessageLogging(EventId = 61162, Level = LogLevel.Error,
        Message = "{owner} states placeholder(s) {keys}, which cannot be filled: the slot file '{filePath}' cannot be read (the message before this one says why). The row is refused rather than read with the placeholder left in its text")]
    public static partial IGenericMessage RowSlotFileUnavailable(ILogger logger, string owner, string filePath, string keys);

    /// <summary>The slot file was read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="filePath">The path APP_SLOT_FILE names.</param>
    /// <param name="count">How many values it states.</param>
    [MessageLogging(EventId = 11590, Level = LogLevel.Information,
        Message = "Read the slot file '{filePath}': {count} value(s) stated")]
    public static partial IGenericMessage Read(ILogger logger, string filePath, int count);

    /// <summary>A placeholder was resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The placeholder's name (never its value).</param>
    /// <param name="source">Where the value came from.</param>
    [MessageLogging(EventId = 11591, Level = LogLevel.Trace,
        Message = "Placeholder '{name}' resolved from {source}")]
    public static partial IGenericMessage PlaceholderResolved(ILogger logger, string name, string source);
}
