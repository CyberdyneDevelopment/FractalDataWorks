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

    /// <summary>Logs a kind that was asked for and has no source to search it.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="kind">The kind with no source.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Error: the host did not register a search for a kind the catalog advertises, so the search cannot claim to have searched it.
    [MessageLogging(EventId = 61642, Level = LogLevel.Error,
        Message = "The catalog search was asked for kind {kind} and no source is registered that searches it")]
    public static partial IGenericMessage NoSourceForKind(ILogger logger, string kind);

    /// <summary>Logs a page that reaches further than a source reads.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="kind">The kind whose source is too narrow.</param>
    /// <param name="window">Skip plus take.</param>
    /// <param name="maximum">The most the source reads.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Error: the endpoint refuses a window past CatalogSearchLimits.MaxWindow, so a source narrower than that limit is a host mistake.
    [MessageLogging(EventId = 91643, Level = LogLevel.Error,
        Message = "The catalog search needs {window} matches of kind {kind} and its source reads at most {maximum}")]
    public static partial IGenericMessage WindowPastSource(ILogger logger, string kind, int window, int maximum);

    /// <summary>Logs a glossary count that came back without a total.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Error: the page was asked for a total and the provider gave none, so the matches cannot be counted.
    [MessageLogging(EventId = 91644, Level = LogLevel.Error,
        Message = "The glossary terms were counted and the provider returned no total")]
    public static partial IGenericMessage TermsNotCounted(ILogger logger);

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
