using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Logging;

/// <summary>
/// MessageLogging for reading an HTTP source page by page in the paging style its connection states (FDW-4938). One message, with its own
/// code, for each reason a paged read is refused or stopped. EventIds: trace 11016-11018, configuration 61010-61018, remote 71007-71012.
/// </summary>
[MessageLoggingTypeCode("HTTP")]
public static partial class HttpPagingLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (11016-11018)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a paged read of an HTTP source is starting in the stated style.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="style">The paging style the connection states.</param>
    /// <param name="pageSize">The page size asked for, or 0 when the style asks for none.</param>
    /// <param name="maxPages">The most pages the read may take.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug, Message = "HTTP connection '{connectionName}' reading in paging style '{style}', page size {pageSize}, at most {maxPages} pages")]
    public static partial IGenericMessage ReadingPaged(ILogger logger, string connectionName, string style, int pageSize, int maxPages);

    /// <summary>Logs that a page of an HTTP source is being requested.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="page">The page's number in this read, from 1.</param>
    /// <param name="url">The page's address.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "HTTP connection '{connectionName}' requesting page {page}: {url}")]
    public static partial IGenericMessage RequestingPage(ILogger logger, string connectionName, int page, string url);

    /// <summary>Logs that a paged read of an HTTP source finished because the remote said it was done.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="pages">The pages read.</param>
    /// <param name="rows">The rows read in all.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information, Message = "HTTP connection '{connectionName}' read {rows} rows in {pages} pages; the remote has no more")]
    public static partial IGenericMessage ReadComplete(ILogger logger, string connectionName, int pages, long rows);

    // ═══════════════════════════════════════════════════════════════════════════
    // Configuration (61010-61018)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a paged read was refused because the connection states no paging style.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61010, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' states no PagingStyle, so it cannot be read page by page; state the style its remote pages by (SingleResponse when it does not page)")]
    public static partial IGenericMessage PagingStyleNotStated(ILogger logger, string connectionName);

    /// <summary>Logs that a paged read was refused because the connection names a paging style no option answers to.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="style">The style it names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61011, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' states PagingStyle '{style}', which is not an HTTP paging style")]
    public static partial IGenericMessage PagingStyleUnknown(ILogger logger, string connectionName, string style);

    /// <summary>Logs that a paged read was refused because the connection leaves out a property its paging style needs.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="style">The style it states.</param>
    /// <param name="property">The additional property it leaves out.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61012, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' pages in style '{style}', which needs the additional property '{property}'; the connection does not state it")]
    public static partial IGenericMessage PagingPropertyMissing(ILogger logger, string connectionName, string style, string property);

    /// <summary>Logs that a paged read was refused because a numeric paging property is not a whole number in its range.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="property">The additional property.</param>
    /// <param name="value">The value it states.</param>
    /// <param name="minimum">The smallest value the property accepts.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61013, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' states '{value}' for '{property}', which must be a whole number of at least {minimum}")]
    public static partial IGenericMessage PagingPropertyNotWholeNumber(ILogger logger, string connectionName, string property, string value, int minimum);

    /// <summary>Logs that a paged read was refused because the paging style declares no default for a property the connection leaves out.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="style">The style it states.</param>
    /// <param name="property">The property neither the connection, the host nor the style gives a value for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61014, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' states no '{property}', and paging style '{style}' declares no positive whole-number default for it")]
    public static partial IGenericMessage PagingDefaultNotDeclared(ILogger logger, string connectionName, string style, string property);

    /// <summary>Logs that a query asking for a window of rows was refused because the stated paging style cannot address that window.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="style">The style it states.</param>
    /// <param name="skip">The rows the query skips.</param>
    /// <param name="reason">Why the style cannot address the window.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61015, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' pages in style '{style}', which cannot ask its remote for the rows of a query that skips {skip}: {reason}")]
    public static partial IGenericMessage PagingWindowNotAddressable(ILogger logger, string connectionName, string style, int skip, string reason);

    /// <summary>Logs that a paged read was refused because the protocol does not carry its query in the request address, where the paging style writes.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="style">The style it states.</param>
    /// <param name="protocol">The connection's protocol.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61016, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' pages in style '{style}', which writes the page into the request address, and protocol '{protocol}' does not carry its query there")]
    public static partial IGenericMessage PagingStyleNotForProtocol(ILogger logger, string connectionName, string style, string protocol);

    /// <summary>Logs that a query was refused because it skips a negative number of rows (FDW-5009).</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="skip">The rows the query skips.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61017, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': a query skips {skip} rows, and a query cannot skip a negative number of rows")]
    public static partial IGenericMessage PagingSkipNegative(ILogger logger, string connectionName, int skip);

    /// <summary>
    /// Logs that a query was refused because it takes fewer rows than its read allows (FDW-5009): a negative number for any read, or none for a
    /// window written into one request, since some remotes read a limit of 0 as their own default page.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="take">The rows the query takes.</param>
    /// <param name="minimum">The fewest rows the read may take.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61018, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': a query takes {take} rows, and this read takes at least {minimum}")]
    public static partial IGenericMessage PagingTakeTooSmall(ILogger logger, string connectionName, int take, int minimum);

    // ═══════════════════════════════════════════════════════════════════════════
    // Remote (71007-71012)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that the remote returned more rows in a page than the page size asked for, so it did not honour the size.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="page">The page's number in this read.</param>
    /// <param name="pageSize">The page size asked for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': page {page} returned more than the {pageSize} rows asked for, so the remote does not honour the page size and the next page cannot be found; the read is stopped")]
    public static partial IGenericMessage PageLargerThanAsked(ILogger logger, string connectionName, int page, int pageSize);

    /// <summary>Logs that the remote's next link or token repeats one this read already followed, which would read the same rows again forever.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="page">The page whose response repeated it.</param>
    /// <param name="next">The repeated link or token.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': page {page} points to '{next}', which this read already followed; the read is stopped")]
    public static partial IGenericMessage NextPageRepeats(ILogger logger, string connectionName, int page, string next);

    /// <summary>Logs that the remote's next link is not an http or https address.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="page">The page whose response held it.</param>
    /// <param name="link">The link as the remote gave it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': page {page} gives the next link '{link}', which is not an http or https address; the read is stopped")]
    public static partial IGenericMessage NextLinkMalformed(ILogger logger, string connectionName, int page, string link);

    /// <summary>Logs that the remote's next link points to another origin, where the connection's credentials and headers must not be sent.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="page">The page whose response held it.</param>
    /// <param name="link">The link.</param>
    /// <param name="origin">The origin the read started on.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': page {page} gives the next link '{link}', which leaves '{origin}'; the connection's credentials are not sent to another origin, so the read is stopped")]
    public static partial IGenericMessage NextLinkLeavesOrigin(ILogger logger, string connectionName, int page, string link, string origin);

    /// <summary>Logs that the remote still had more after the most pages a read may take.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="style">The style it states.</param>
    /// <param name="maxPages">The most pages a read may take.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}' (paging style '{style}') still had more rows after {maxPages} pages, the most a read may take (PagingMaxPages); the read is stopped rather than returned as complete")]
    public static partial IGenericMessage MaxPagesExceeded(ILogger logger, string connectionName, string style, int maxPages);

    /// <summary>Logs that a page's body could not be read for the next link or token, so the next page cannot be found.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The HTTP connection's name.</param>
    /// <param name="page">The page whose body could not be read.</param>
    /// <param name="path">The path the style reads.</param>
    /// <param name="reason">Why the body could not be read there.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': the body of page {page} could not be read at '{path}': {reason}; the read is stopped")]
    public static partial IGenericMessage PageBodyUnreadable(ILogger logger, string connectionName, int page, string path, string reason);
}
