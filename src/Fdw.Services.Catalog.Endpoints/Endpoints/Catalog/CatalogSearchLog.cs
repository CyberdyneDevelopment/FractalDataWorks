using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>MessageLogging for the catalog search (FDW-1681).</summary>
[MessageLoggingTypeCode("CATALOG")]
public static partial class CatalogSearchLog
{
    /// <summary>Logs how many entries a catalog search matched.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="text">The text searched for; empty for a browse.</param>
    /// <param name="kinds">How many kinds were searched.</param>
    /// <param name="total">How many entries matched before paging.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug: one outcome per request, which an operator reads only when tracing a slow or surprising search.
    [MessageLogging(EventId = 11640, Level = LogLevel.Debug,
        Message = "Catalog search for '{text}' over {kinds} kinds matched {total} entries")]
    public static partial IGenericMessage Searched(ILogger logger, string text, int kinds, int total);

    /// <summary>Logs how many things a where-used question found.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="kind">The kind of the thing asked about.</param>
    /// <param name="name">Its name.</param>
    /// <param name="direction">Which way the question was read.</param>
    /// <param name="count">How many things were found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Debug: one outcome per request, as for the search.
    [MessageLogging(EventId = 11641, Level = LogLevel.Debug,
        Message = "Catalog uses of {kind} '{name}' read {direction}: {count} found")]
    public static partial IGenericMessage UsesAnswered(ILogger logger, string kind, string name, string direction, int count);
}
