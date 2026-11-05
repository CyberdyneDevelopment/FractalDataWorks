using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags.Logging;

/// <summary>MessageLogging for the tags service (FDW-3669).</summary>
[MessageLoggingTypeCode("TAGS")]
public static partial class TagsLog
{
    /// <summary>Logs that a tag is being put on a thing.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tagName">The tag's name.</param>
    /// <param name="kind">The kind of the thing.</param>
    /// <param name="thingId">The thing's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11800, Level = LogLevel.Information, Message = "Putting tag '{tagName}' on {kind} {thingId}")]
    public static partial IGenericMessage Linking(ILogger logger, string tagName, string kind, Guid thingId);

    /// <summary>Logs that a tag was put on a thing.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tagName">The tag's name.</param>
    /// <param name="kind">The kind of the thing.</param>
    /// <param name="thingId">The thing's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11801, Level = LogLevel.Information, Message = "Tag '{tagName}' is now on {kind} {thingId}")]
    public static partial IGenericMessage Linked(ILogger logger, string tagName, string kind, Guid thingId);

    /// <summary>Logs that a tag was taken off a thing.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tagName">The tag's name.</param>
    /// <param name="kind">The kind of the thing.</param>
    /// <param name="thingId">The thing's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11802, Level = LogLevel.Information, Message = "Tag '{tagName}' was taken off {kind} {thingId}")]
    public static partial IGenericMessage Unlinked(ILogger logger, string tagName, string kind, Guid thingId);

    /// <summary>Logs that the links of a tag are being retired before the tag is deleted.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tagName">The tag's name.</param>
    /// <param name="kind">The kind whose links are being retired.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11803, Level = LogLevel.Information, Message = "Retiring the {kind} links of tag '{tagName}'")]
    public static partial IGenericMessage RetiringLinks(ILogger logger, string tagName, string kind);
}
