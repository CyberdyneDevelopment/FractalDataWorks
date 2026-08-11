using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Components.Logging;

/// <summary>
/// MessageLogging for ApiKeyProvider operations.
/// EventId range: 4470-4489
/// </summary>
[MessageLoggingTypeCode("COMPONENTS5")]
public static partial class ApiKeyProviderLog
{
    /// <summary>
    /// Logs that personal access tokens and agent keys are being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Loading personal access tokens and agent keys")]
    public static partial IGenericMessage LoadingTokens(ILogger logger);

    /// <summary>
    /// Logs that the given number of personal access tokens were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of personal access tokens loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Loaded {count} personal access tokens")]
    public static partial IGenericMessage LoadedTokens(ILogger logger, int count);

    /// <summary>
    /// Logs that loading personal access tokens failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to load personal access tokens")]
    public static partial IGenericMessage LoadTokensFailed(ILogger logger);

    /// <summary>
    /// Logs that the given number of agent keys were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of agent keys loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Loaded {count} agent keys")]
    public static partial IGenericMessage LoadedAgentKeys(ILogger logger, int count);

    /// <summary>
    /// Logs that loading agent keys failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to load agent keys")]
    public static partial IGenericMessage LoadAgentKeysFailed(ILogger logger);

    // Why (FDW-583): pre-action announcement, not a completed business milestone — noise at Info.
    /// <summary>
    /// Logs that a personal access token with the given label is being created.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="label">The label of the personal access token being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Creating personal access token '{label}'")]
    public static partial IGenericMessage CreatingToken(ILogger logger, string label);

    /// <summary>
    /// Logs that a personal access token with the given label was created successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="label">The label of the personal access token that was created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Personal access token '{label}' created successfully")]
    public static partial IGenericMessage TokenCreated(ILogger logger, string label);

    /// <summary>
    /// Logs that creating a personal access token with the given label failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="label">The label of the personal access token that failed to be created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to create personal access token '{label}'")]
    public static partial IGenericMessage CreateTokenFailed(ILogger logger, string label);

    /// <summary>
    /// Logs that an exception occurred while creating a personal access token with the given label.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that occurred while creating the personal access token.</param>
    /// <param name="label">The label of the personal access token being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Exception creating personal access token '{label}'")]
    public static partial IGenericMessage CreateTokenException(ILogger logger, Exception exception, string label);

    // Why (FDW-583): pre-action announcement, not a completed business milestone — noise at Info.
    /// <summary>
    /// Logs that the personal access token with the given id is being revoked.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tokenId">The identifier of the personal access token being revoked.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Revoking personal access token '{tokenId}'")]
    public static partial IGenericMessage RevokingToken(ILogger logger, Guid tokenId);

    /// <summary>
    /// Logs that the personal access token with the given id was revoked successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tokenId">The identifier of the personal access token that was revoked.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Personal access token '{tokenId}' revoked successfully")]
    public static partial IGenericMessage TokenRevoked(ILogger logger, Guid tokenId);

    /// <summary>
    /// Logs that revoking the personal access token with the given id failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="tokenId">The identifier of the personal access token that failed to be revoked.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to revoke personal access token '{tokenId}'")]
    public static partial IGenericMessage RevokeTokenFailed(ILogger logger, Guid tokenId);

    // Why (FDW-583): pre-action announcement, not a completed business milestone — noise at Info.
    /// <summary>
    /// Logs that an agent key with the given label is being created.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="label">The label of the agent key being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Creating agent key '{label}'")]
    public static partial IGenericMessage CreatingAgentKey(ILogger logger, string label);

    /// <summary>
    /// Logs that an agent key with the given label was created successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="label">The label of the agent key that was created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Agent key '{label}' created successfully")]
    public static partial IGenericMessage AgentKeyCreated(ILogger logger, string label);

    /// <summary>
    /// Logs that creating an agent key with the given label failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="label">The label of the agent key that failed to be created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Failed to create agent key '{label}'")]
    public static partial IGenericMessage CreateAgentKeyFailed(ILogger logger, string label);

    /// <summary>
    /// Logs that an exception occurred while creating an agent key with the given label.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that occurred while creating the agent key.</param>
    /// <param name="label">The label of the agent key being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Exception creating agent key '{label}'")]
    public static partial IGenericMessage CreateAgentKeyException(ILogger logger, Exception exception, string label);

    // Why (FDW-583): pre-action announcement, not a completed business milestone — noise at Info.
    /// <summary>
    /// Logs that the agent key with the given id is being deleted.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyId">The identifier of the agent key being deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Deleting agent key '{keyId}'")]
    public static partial IGenericMessage DeletingAgentKey(ILogger logger, Guid keyId);

    /// <summary>
    /// Logs that the agent key with the given id was deleted successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyId">The identifier of the agent key that was deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Agent key '{keyId}' deleted successfully")]
    public static partial IGenericMessage AgentKeyDeleted(ILogger logger, Guid keyId);

    /// <summary>
    /// Logs that deleting the agent key with the given id failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyId">The identifier of the agent key that failed to be deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "Failed to delete agent key '{keyId}'")]
    public static partial IGenericMessage DeleteAgentKeyFailed(ILogger logger, Guid keyId);
}
