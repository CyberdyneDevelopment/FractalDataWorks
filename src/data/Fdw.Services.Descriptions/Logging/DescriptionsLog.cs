using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Descriptions.Logging;

/// <summary>MessageLogging for the descriptions service (FDW-6120).</summary>
[MessageLoggingTypeCode("DESC")]
public static partial class DescriptionsLog
{
    /// <summary>Logs that a description is being saved.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="key">The description's key as text.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11900, Level = LogLevel.Trace, Message = "Saving the description '{key}'")]
    public static partial IGenericMessage Saving(ILogger logger, string key);

    /// <summary>Logs that a description was saved.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="key">The description's key as text.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11901, Level = LogLevel.Information, Message = "The description '{key}' was saved")]
    public static partial IGenericMessage Saved(ILogger logger, string key);

    /// <summary>Logs that a description was deleted.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="key">The description's key as text.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11902, Level = LogLevel.Information, Message = "The description '{key}' was deleted")]
    public static partial IGenericMessage Deleted(ILogger logger, string key);

    /// <summary>Logs that the descriptions of a set of things are being read for a fill.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="type">The description type.</param>
    /// <param name="count">How many keys the read names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11905, Level = LogLevel.Trace, Message = "Reading the {type} descriptions of {count} things for a fill")]
    public static partial IGenericMessage ReadingMany(ILogger logger, string type, int count);

    /// <summary>Logs that a description type states no read permission, which stops the host when it initializes.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="type">The description type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60343, Level = LogLevel.Critical,
        Message = "The description type '{type}' states no read permission; every description type states the permission that reads a thing of it, because a read of a thing of that type is asked the platform's authorization with it; fix the type's declaration; the host does not start",
        ClientMessage = "A description type states no read permission, so the host does not start")]
    public static partial IGenericMessage ReadPermissionNotStated(ILogger logger, string type);

    /// <summary>Logs that the part length a host declared cannot hold a character, which stops the host when it registers the Descriptions service type.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="partLength">The part length declared.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60344, Level = LogLevel.Critical,
        Message = "The description part length is {partLength}; a part holds at least one character; set DescriptionServiceTypes.PartLength to 1 or more before the Descriptions service type is registered; the host does not start",
        ClientMessage = "The description part length is not valid, so the host does not start")]
    public static partial IGenericMessage PartLengthInvalid(ILogger logger, int partLength);

    /// <summary>Logs that a description is being saved as more than one part.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="key">The description's key as text.</param>
    /// <param name="parts">How many parts the text was split into.</param>
    /// <param name="removed">How many parts above those the description had before, which the save removes.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11907, Level = LogLevel.Trace, Message = "The description '{key}' is saved as {parts} part(s), and {removed} part(s) it had above those are removed")]
    public static partial IGenericMessage SavedAsParts(ILogger logger, string key, int parts, int removed);

    /// <summary>Logs how the descriptions of an owner's things of one type were kept in step with the owner's save (FDW-6589).</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="type">The description type.</param>
    /// <param name="ownerId">The owner's logical Id.</param>
    /// <param name="things">How many things the save handed over.</param>
    /// <param name="written">How many descriptions were new or changed, and written.</param>
    /// <param name="removed">How many description parts were removed: things no longer held, emptied texts, and parts above a shorter text.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11906, Level = LogLevel.Trace, Message = "The {type} descriptions owned by {ownerId}: {things} thing(s) handed over, {written} written, {removed} part(s) removed")]
    public static partial IGenericMessage DescribedOwned(ILogger logger, string type, Guid ownerId, int things, int written, int removed);
}
