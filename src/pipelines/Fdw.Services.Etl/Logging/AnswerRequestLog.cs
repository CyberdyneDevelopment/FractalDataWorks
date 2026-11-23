using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the answer requests of runs (FDW-6142): asking, answering and starting the continuation, and the Ask component's own refusals. Every refusal is logged and returned in the failure result,
/// numbered in the category of what it says: a request that is not valid (2), one that does not exist (3), one in a state that forbids it (4), a caller who may not (5), a host that is not set up (6) and a store that
/// failed (7); what happened is information (1). EventId range: 21402 to 21440, 21455 to 21458, 31405, 31431, 41404 to 41409, 41439, 51410 to 51412, 60413, 71414 to 71420, 10421 to 10426 and 21450.
/// </summary>
[MessageLoggingTypeCode("ANSWERREQUEST")]
public static partial class AnswerRequestLog
{
    /// <summary>Logs an answer that is blank.</summary>
    [MessageLogging(EventId = 21402, Level = LogLevel.Error,
        Message = "Answer request {requestId} was answered with no text",
        ClientMessage = "An answer cannot be blank")]
    public static partial IGenericMessage AnswerEmpty(ILogger logger, Guid requestId);

    /// <summary>Logs an answer to a choice that is none of its options.</summary>
    [MessageLogging(EventId = 21403, Level = LogLevel.Error,
        Message = "Answer request {requestId} is a choice and its answer is none of the options it shows",
        ClientMessage = "The answer must be one of the options shown")]
    public static partial IGenericMessage AnswerNotAnOption(ILogger logger, Guid requestId);

    /// <summary>Logs a second answer that differs from the one taken.</summary>
    [MessageLogging(EventId = 41404, Level = LogLevel.Error,
        Message = "Answer request {requestId} was answered before and its continuation has not started; the answer stands and cannot be changed",
        ClientMessage = "That request was answered already; the answer cannot be changed")]
    public static partial IGenericMessage AnswerDiffers(ILogger logger, Guid requestId);

    /// <summary>Logs a request that does not exist for the caller.</summary>
    [MessageLogging(EventId = 31405, Level = LogLevel.Error,
        Message = "Answer request {requestId} is not a request of the caller's tenant",
        ClientMessage = "That request does not exist")]
    public static partial IGenericMessage NotFound(ILogger logger, Guid requestId);

    /// <summary>Logs a request that was answered already, so a second answer starts nothing.</summary>
    [MessageLogging(EventId = 41406, Level = LogLevel.Error,
        Message = "Answer request {requestId} is {state} and takes no answer; nothing was started",
        ClientMessage = "That request was answered already")]
    public static partial IGenericMessage AlreadyAnswered(ILogger logger, Guid requestId, string state);

    /// <summary>Logs a request whose time has passed.</summary>
    [MessageLogging(EventId = 41407, Level = LogLevel.Error,
        Message = "Answer request {requestId} ended at {expiresAt} and takes no answer; nothing was started",
        ClientMessage = "That request has expired")]
    public static partial IGenericMessage Expired(ILogger logger, Guid requestId, DateTimeOffset expiresAt);

    /// <summary>Logs a continuation whose start was made before and failed, which an answer does not try again.</summary>
    [MessageLogging(EventId = 41408, Level = LogLevel.Error,
        Message = "The continuation of answer request {requestId} was claimed by run {executionId} and its start failed there, so it is not started again",
        ClientMessage = "The start of that answer failed before; it is not tried a second time")]
    public static partial IGenericMessage StartFailedBefore(ILogger logger, Guid requestId, Guid executionId);

    /// <summary>Logs a continuation whose start was claimed and never recorded how it ended.</summary>
    [MessageLogging(EventId = 41409, Level = LogLevel.Error,
        Message = "The continuation of answer request {requestId} was claimed by run {executionId} and never recorded how it ended; whether it started cannot be known, so it is not started again",
        ClientMessage = "That answer may have started its pipeline, and nobody can say how it ended; it is not started a second time")]
    public static partial IGenericMessage StartOutcomeUnknown(ILogger logger, Guid requestId, Guid executionId);

    /// <summary>Logs a call that needs a person and was made by a caller that is none.</summary>
    [MessageLogging(EventId = 51410, Level = LogLevel.Error,
        Message = "Answer requests were asked for ({operation}) by a caller that is no person, so there is nobody they could belong to",
        ClientMessage = "Only a signed-in person can use answer requests")]
    public static partial IGenericMessage CallerIsNoPerson(ILogger logger, string operation);

    /// <summary>Logs a call made by a caller whose token states no tenant.</summary>
    [MessageLogging(EventId = 51411, Level = LogLevel.Error,
        Message = "Answer requests were asked for ({operation}) by a caller whose token states no active tenant",
        ClientMessage = "Your sign-in states no tenant")]
    public static partial IGenericMessage CallerHasNoTenant(ILogger logger, string operation);

    /// <summary>Logs an answer by a caller who is not the person the request was kept for.</summary>
    [MessageLogging(EventId = 51412, Level = LogLevel.Error,
        Message = "Answer request {requestId} was kept for another person than the caller, so the caller may not use it",
        ClientMessage = "That request is not yours")]
    public static partial IGenericMessage NotYours(ILogger logger, Guid requestId);

    /// <summary>Logs a host that states no operational store.</summary>
    [MessageLogging(EventId = 60413, Level = LogLevel.Error,
        Message = "Answer requests ({operation}) are kept in the operational store, and this host states none")]
    public static partial IGenericMessage OperationalStoreNotNamed(ILogger logger, string operation);

    /// <summary>Logs a request that could not be written.</summary>
    [MessageLogging(EventId = 71414, Level = LogLevel.Error,
        Message = "The answer request of component '{component}' could not be kept: {reason}")]
    public static partial IGenericMessage NotKept(ILogger logger, string component, string? reason);

    /// <summary>Logs a request, or its parts, that could not be read.</summary>
    [MessageLogging(EventId = 71415, Level = LogLevel.Error,
        Message = "Answer request data ({what}) could not be read: {reason}")]
    public static partial IGenericMessage NotRead(ILogger logger, string what, string? reason);

    /// <summary>Logs an answer that could not be recorded.</summary>
    [MessageLogging(EventId = 71416, Level = LogLevel.Error,
        Message = "The answer to request {requestId} could not be recorded: {reason}")]
    public static partial IGenericMessage AnswerNotRecorded(ILogger logger, Guid requestId, string? reason);

    /// <summary>Logs a continuation that started and whose request could not be moved to Started.</summary>
    [MessageLogging(EventId = 71417, Level = LogLevel.Error,
        Message = "The continuation of answer request {requestId} started as run {continuationId} and the request could not be recorded as Started: {reason}")]
    public static partial IGenericMessage StartNotRecorded(ILogger logger, Guid requestId, Guid continuationId, string? reason);

    /// <summary>Logs a person who could not be told about a request, because the write of the fact failed.</summary>
    [MessageLogging(EventId = 71418, Level = LogLevel.Error,
        Message = "That the person was told about answer request {requestId} could not be recorded: {reason}")]
    public static partial IGenericMessage NotAnnouncedRecorded(ILogger logger, Guid requestId, string? reason);

    /// <summary>Logs a notification that was not sent.</summary>
    [MessageLogging(EventId = 71419, Level = LogLevel.Error,
        Message = "The person was not told about answer request {requestId} through rule '{rule}': {reason}; the request stays open and is listed to them")]
    public static partial IGenericMessage AnnouncementFailed(ILogger logger, Guid requestId, string rule, string? reason);

    /// <summary>Logs a claim of a continuation's start that could not be made or read.</summary>
    [MessageLogging(EventId = 71420, Level = LogLevel.Error,
        Message = "The start of the continuation of answer request {requestId} could not be claimed: {reason}")]
    public static partial IGenericMessage StartNotClaimed(ILogger logger, Guid requestId, string? reason);

    /// <summary>Logs a request that was kept.</summary>
    [MessageLogging(EventId = 10421, Level = LogLevel.Information,
        Message = "Answer request {requestId} was kept for person {personId} by component '{component}' in run {executionId}, open until {expiresAt}")]
    public static partial IGenericMessage Kept(ILogger logger, Guid requestId, Guid personId, string component, Guid executionId, DateTimeOffset expiresAt);

    /// <summary>Logs a step that reached a request it kept before.</summary>
    [MessageLogging(EventId = 10422, Level = LogLevel.Information,
        Message = "Component '{component}' in run {executionId} had asked before; answer request {requestId} stands and no second one was kept")]
    public static partial IGenericMessage AlreadyKept(ILogger logger, string component, Guid executionId, Guid requestId);

    /// <summary>Logs an answer that was taken.</summary>
    [MessageLogging(EventId = 10423, Level = LogLevel.Information,
        Message = "Answer request {requestId} was answered by person {personId}")]
    public static partial IGenericMessage Answered(ILogger logger, Guid requestId, Guid personId);

    /// <summary>Logs a continuation that was started from an answer.</summary>
    [MessageLogging(EventId = 10424, Level = LogLevel.Information,
        Message = "Answer request {requestId} started its continuation as run {continuationId}")]
    public static partial IGenericMessage Started(ILogger logger, Guid requestId, Guid continuationId);

    /// <summary>Logs requests that passed their end.</summary>
    [MessageLogging(EventId = 10425, Level = LogLevel.Information,
        Message = "{count} answer request(s) of person {personId} passed their end and are now Expired; they start nothing")]
    public static partial IGenericMessage ExpiredNow(ILogger logger, int count, Guid personId);

    /// <summary>Logs an Ask component that judged nothing and kept no request because the run is a test run.</summary>
    [MessageLogging(EventId = 10426, Level = LogLevel.Information,
        Message = "Component '{component}' asks a person to answer and this is a test run, so it judged nothing, kept no request and fired no output")]
    public static partial IGenericMessage AskSkippedInATest(ILogger logger, string component);

    /// <summary>Logs an Ask component run by a run that acts as no person.</summary>
    [MessageLogging(EventId = 21430, Level = LogLevel.Error,
        Message = "Component '{component}' asks a person to answer and the run acts as no person, so there is nobody to ask",
        ClientMessage = "That step needs a run that acts as a person")]
    public static partial IGenericMessage RunHasNoPerson(ILogger logger, string component);

    /// <summary>Logs a continuation that is no pipeline the run can see.</summary>
    [MessageLogging(EventId = 31431, Level = LogLevel.Error,
        Message = "Component '{component}' states pipeline '{pipeline}' as the continuation and it could not be found: {reason}",
        ClientMessage = "The continuation pipeline does not exist")]
    public static partial IGenericMessage ContinuationNotFound(ILogger logger, string component, string pipeline, string? reason);

    /// <summary>Logs a continuation that is the pipeline asking.</summary>
    [MessageLogging(EventId = 21432, Level = LogLevel.Error,
        Message = "Component '{component}' states pipeline '{pipeline}' as its own continuation; a pipeline cannot be started under a run of itself",
        ClientMessage = "A pipeline cannot be its own continuation")]
    public static partial IGenericMessage ContinuationIsTheAskingPipeline(ILogger logger, string component, string pipeline);

    /// <summary>Logs a choice with no options.</summary>
    [MessageLogging(EventId = 21434, Level = LogLevel.Error,
        Message = "Component '{component}' asks a choice and states no options",
        ClientMessage = "A choice needs options")]
    public static partial IGenericMessage ChoiceNeedsOptions(ILogger logger, string component);

    /// <summary>Logs a question that states options.</summary>
    [MessageLogging(EventId = 21435, Level = LogLevel.Error,
        Message = "Component '{component}' asks a question and states options, which only a choice takes",
        ClientMessage = "Only a choice takes options")]
    public static partial IGenericMessage QuestionTakesNoOptions(ILogger logger, string component);

    /// <summary>Logs an argument that has the same name as the parameter that receives the answer.</summary>
    [MessageLogging(EventId = 21436, Level = LogLevel.Error,
        Message = "Component '{component}' states an argument '{name}', which is the parameter the answer goes to",
        ClientMessage = "An argument cannot have the same name as the answer's parameter")]
    public static partial IGenericMessage ArgumentNamedLikeTheAnswer(ILogger logger, string component, string name);

    /// <summary>Logs a grant variable that holds no grant id.</summary>
    [MessageLogging(EventId = 21437, Level = LogLevel.Error,
        Message = "Component '{component}' reads the stored grant from variable '{variable}', which holds '{text}' and not a grant id",
        ClientMessage = "The grant is not readable")]
    public static partial IGenericMessage GrantVariableNotAGrant(ILogger logger, string component, string variable, string text);

    /// <summary>Logs a text that could not be made from the variables.</summary>
    [MessageLogging(EventId = 21438, Level = LogLevel.Error,
        Message = "Component '{component}' could not make the words of its request: {reason}",
        ClientMessage = "The request's words could not be made")]
    public static partial IGenericMessage TextNotRendered(ILogger logger, string component, string reason);

    /// <summary>Logs a call the policy refuses.</summary>
    [MessageLogging(EventId = 41439, Level = LogLevel.Error,
        Message = "Component '{component}': policy '{policy}' refuses call '{call}'",
        ClientMessage = "That call is refused by the policy")]
    public static partial IGenericMessage CallRefused(ILogger logger, string component, string policy, string call);

    /// <summary>Logs a component whose name starts with the prefix of the claim of an answered step.</summary>
    [MessageLogging(EventId = 21450, Level = LogLevel.Error,
        Message = "Component '{component}' has a name that starts with '{prefix}', which is how the claim of an answered step is called, so it could collide with that claim",
        ClientMessage = "A component's name cannot start with that prefix")]
    public static partial IGenericMessage ComponentNameReserved(ILogger logger, string component, string prefix);

    /// <summary>Logs an ask whose policy states no positive time the request stays open.</summary>
    [MessageLogging(EventId = 21455, Level = LogLevel.Error,
        Message = "Component '{component}' asks under policy '{policy}', which states no positive time a request stays open",
        ClientMessage = "The policy states no time a request stays open")]
    public static partial IGenericMessage ExpiryNotPositive(ILogger logger, string component, string policy);

    /// <summary>Logs an ask that leaves out the component, policy, call or answer parameter.</summary>
    [MessageLogging(EventId = 21456, Level = LogLevel.Error,
        Message = "The ask of run {executionId} leaves out its component, its policy, its call or the parameter the answer goes to, and none is assumed",
        ClientMessage = "The request cannot be kept: something it needs is not stated")]
    public static partial IGenericMessage AskNamesMissing(ILogger logger, Guid executionId);

    /// <summary>Logs an ask that shows the person no title.</summary>
    [MessageLogging(EventId = 21457, Level = LogLevel.Error,
        Message = "Component '{component}' asks and shows the person no title",
        ClientMessage = "A request needs a title")]
    public static partial IGenericMessage NoTitleShown(ILogger logger, string component);

    /// <summary>Logs an ask whose arguments repeat a name.</summary>
    [MessageLogging(EventId = 21458, Level = LogLevel.Error,
        Message = "Component '{component}' states two arguments with the same name",
        ClientMessage = "Two arguments have the same name")]
    public static partial IGenericMessage ArgumentsRepeated(ILogger logger, string component);

    /// <summary>Logs a kind that no collection option declares.</summary>
    [MessageLogging(EventId = 21440, Level = LogLevel.Error,
        Message = "Component '{component}' states request kind '{kind}', which is none of the kinds",
        ClientMessage = "That request kind does not exist")]
    public static partial IGenericMessage KindUnknown(ILogger logger, string component, string kind);
}
