using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>The coded messages the catalog endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("CATALOGANSWERS")]
public static partial class CatalogAnswerLog
{

    /// <summary>Logs that The request to save glossary term version was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Warning, Message = "The request to save glossary term version was refused: the caller is not signed in", ClientMessage = "You must be signed in to save glossary term version")]
    public static partial IGenericMessage SaveGlossaryTermVersionNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to delete glossary term version was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50002, Level = LogLevel.Warning, Message = "The request to delete glossary term version was refused: the caller is not signed in", ClientMessage = "You must be signed in to delete glossary term version")]
    public static partial IGenericMessage DeleteGlossaryTermVersionNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to list glossary term versions was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50003, Level = LogLevel.Warning, Message = "The request to list glossary term versions was refused: the caller is not signed in", ClientMessage = "You must be signed in to list glossary term versions")]
    public static partial IGenericMessage ListGlossaryTermVersionsNotSignedIn(ILogger logger);

    /// <summary>Logs that Glossary term was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Glossary term was not found, or is not visible to the caller", ClientMessage = "Glossary term was not found")]
    public static partial IGenericMessage UpdateGlossaryTermGlossarytermNotFound(ILogger logger);

    /// <summary>Logs that Data set annotation was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Data set annotation was not found, or is not visible to the caller", ClientMessage = "Data set annotation was not found")]
    public static partial IGenericMessage GetDataSetAnnotationDatasetannotationNotFound(ILogger logger);

    /// <summary>Logs that Glossary term was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "Glossary term was not found, or is not visible to the caller", ClientMessage = "Glossary term was not found")]
    public static partial IGenericMessage GetGlossaryTermGlossarytermNotFound(ILogger logger);

    /// <summary>Logs that A catalog search named 'unknown', which is not a catalog kind (the kinds are kinds).</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning, Message = "A catalog search named '{unknown}', which is not a catalog kind (the kinds are {kinds})", ClientMessage = "'{unknown}' is not a catalog kind; the kinds are {kinds}")]
    public static partial IGenericMessage SearchCatalogUnknownKind(ILogger logger, string unknown, string kinds);

    /// <summary>Logs that The paging of a catalog search answered a success and no value.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The paging of a catalog search answered a success and no value", ClientMessage = "The catalog search could not be paged")]
    public static partial IGenericMessage SearchCatalogPagingReturnedNothing(ILogger logger);

    /// <summary>Logs that A catalog search asked for skip plus take beyond maxWindow.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning, Message = "A catalog search asked for skip plus take beyond {maxWindow}", ClientMessage = "skip plus take must be at most {maxWindow}; narrow the search with q or entityTypes to reach further")]
    public static partial IGenericMessage SearchCatalogWindowTooLarge(ILogger logger, long maxWindow);

    /// <summary>Logs that A catalog-uses request named a kind that is not one of kinds.</summary>
    [MessageLogging(EventId = 20003, Level = LogLevel.Warning, Message = "A catalog-uses request named a kind that is not one of {kinds}", ClientMessage = "kind must be one of {kinds}")]
    public static partial IGenericMessage GetCatalogUsesKindNotOffered(ILogger logger, string kinds);

    /// <summary>Logs that A catalog-uses request named no name.</summary>
    [MessageLogging(EventId = 20004, Level = LogLevel.Warning, Message = "A catalog-uses request named no name", ClientMessage = "name is required")]
    public static partial IGenericMessage GetCatalogUsesNameRequired(ILogger logger);

    /// <summary>Logs that A catalog-uses request named a direction that is not one of directions.</summary>
    [MessageLogging(EventId = 20005, Level = LogLevel.Warning, Message = "A catalog-uses request named a direction that is not one of {directions}", ClientMessage = "direction must be one of {directions}")]
    public static partial IGenericMessage GetCatalogUsesDirectionNotOffered(ILogger logger, string directions);
}
