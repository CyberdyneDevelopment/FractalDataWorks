using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Logging;

/// <summary>
/// MessageLogging for retention policies, legal holds and the retention job (FDW-1575).
/// EventId ranges: 13100-13105 (done), 23100-23101 (bad request, 400), 33100-33101 (not found, 404), 43100 (conflict, 409),
/// 73100-73103 (store and document failures, 500).
/// </summary>
[MessageLoggingTypeCode("MULTITENANCY")]
public static partial class RetentionLog
{
    /// <summary>Logs that a retention policy was set.</summary>
    [MessageLogging(EventId = 13100, Level = LogLevel.Information, Message = "Retention policy for '{documentType}' of organization '{organizationId}' was set: {years} years, {months} months, {days} days from '{startField}', then '{endAction}'")]
    public static partial IGenericMessage PolicySet(ILogger logger, string documentType, Guid organizationId, int years, int months, int days, string startField, string endAction);

    /// <summary>Logs that a retention policy was removed.</summary>
    [MessageLogging(EventId = 13101, Level = LogLevel.Information, Message = "Retention policy for '{documentType}' of organization '{organizationId}' was removed")]
    public static partial IGenericMessage PolicyRemoved(ILogger logger, string documentType, Guid organizationId);

    /// <summary>Logs that a legal hold was placed.</summary>
    [MessageLogging(EventId = 13102, Level = LogLevel.Information, Message = "Document '{documentId}' of organization '{organizationId}' was put under a legal hold")]
    public static partial IGenericMessage HoldPlaced(ILogger logger, string documentId, Guid organizationId);

    /// <summary>Logs that a legal hold was lifted.</summary>
    [MessageLogging(EventId = 13103, Level = LogLevel.Information, Message = "The legal hold on document '{documentId}' of organization '{organizationId}' was lifted")]
    public static partial IGenericMessage HoldLifted(ILogger logger, string documentId, Guid organizationId);

    /// <summary>Logs that the retention job ended a document.</summary>
    [MessageLogging(EventId = 13104, Level = LogLevel.Information, Message = "Document '{documentId}' ({documentType}) of organization '{organizationId}' was ended with '{endAction}'")]
    public static partial IGenericMessage DocumentEnded(ILogger logger, string documentId, string documentType, Guid organizationId, string endAction);

    /// <summary>Logs the totals of a retention run.</summary>
    [MessageLogging(EventId = 13105, Level = LogLevel.Information, Message = "Retention run for organization '{organizationId}' considered {considered} documents: {ended} ended, {held} held, {undated} without a start date, {failed} failed")]
    public static partial IGenericMessage RunFinished(ILogger logger, Guid organizationId, int considered, int ended, int held, int undated, int failed);

    /// <summary>Logs that a retention policy was refused as stated.</summary>
    [MessageLogging(EventId = 23100, Level = LogLevel.Warning, Message = "The retention policy was refused: {reason}")]
    public static partial IGenericMessage PolicyInvalid(ILogger logger, string reason);

    /// <summary>Logs that a legal hold was refused as stated.</summary>
    [MessageLogging(EventId = 23101, Level = LogLevel.Warning, Message = "The legal hold was refused: {reason}")]
    public static partial IGenericMessage HoldInvalid(ILogger logger, string reason);

    /// <summary>Logs that an organization has no retention policy for a document type.</summary>
    [MessageLogging(EventId = 33100, Level = LogLevel.Warning, Message = "Organization '{organizationId}' has no retention policy for '{documentType}'")]
    public static partial IGenericMessage PolicyNotFound(ILogger logger, string documentType, Guid organizationId);

    /// <summary>Logs that a document is not under a legal hold.</summary>
    [MessageLogging(EventId = 33101, Level = LogLevel.Warning, Message = "Document '{documentId}' of organization '{organizationId}' is not under a legal hold")]
    public static partial IGenericMessage HoldNotFound(ILogger logger, string documentId, Guid organizationId);

    /// <summary>Logs that a document is already under a legal hold.</summary>
    [MessageLogging(EventId = 43100, Level = LogLevel.Warning, Message = "Document '{documentId}' of organization '{organizationId}' is already under a legal hold")]
    public static partial IGenericMessage AlreadyHeld(ILogger logger, string documentId, Guid organizationId);

    /// <summary>Logs that a read from the store failed.</summary>
    [MessageLogging(EventId = 73100, Level = LogLevel.Error, Message = "{what} could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that a write to the store failed.</summary>
    [MessageLogging(EventId = 73101, Level = LogLevel.Error, Message = "{what} could not be written: {reason}")]
    public static partial IGenericMessage WriteFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that the application could not list the documents of a type.</summary>
    [MessageLogging(EventId = 73102, Level = LogLevel.Error, Message = "The documents of type '{documentType}' of organization '{organizationId}' could not be listed: {reason}")]
    public static partial IGenericMessage DocumentsNotListed(ILogger logger, string documentType, Guid organizationId, string? reason);

    /// <summary>Logs that the application could not end a document.</summary>
    [MessageLogging(EventId = 73103, Level = LogLevel.Warning, Message = "Document '{documentId}' of organization '{organizationId}' could not be ended with '{endAction}': {reason}")]
    public static partial IGenericMessage DocumentNotEnded(ILogger logger, string documentId, Guid organizationId, string endAction, string? reason);
}
