using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ClientModels = Fdw.Services.Messaging.Clients.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging.Clients;

/// <summary>
/// HTTP client for the messaging API endpoints.
/// Shared between ManagementUI (MudBlazor) and ManagementUI-Tailwind skins.
/// </summary>
/// <remarks>
/// Headless chain:
/// <list type="bullet">
///   <item><description>Consumer (before): MessageProvider (Fdw.Services.Messaging.Components) wraps this client</description></item>
///   <item><description>This (client): <see cref="MessageApiClient"/> — owns HTTP communication with the messaging API</description></item>
/// </list>
/// </remarks>
public class MessageApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MessageApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageApiClient"/> class.
    /// </summary>
    public MessageApiClient(HttpClient httpClient, ILogger<MessageApiClient>? logger)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<MessageApiClient>.Instance;
    }

    /// <summary>Gets a filtered, paged list of messages.</summary>
    /// <returns>The matching messages.</returns>
    public virtual async Task<IReadOnlyList<ClientModels.MessagePayload>> GetMessages(
        string? messageType = null,
        string? severity = null,
        string? status = null,
        string? referenceId = null,
        Guid? after = null,
        Guid? before = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = $"messages?skip={skip}&take={take}";
        if (!string.IsNullOrEmpty(messageType))
        {
            query += $"&messageType={Uri.EscapeDataString(messageType)}";
        }
        if (!string.IsNullOrEmpty(severity))
        {
            query += $"&severity={Uri.EscapeDataString(severity)}";
        }
        if (!string.IsNullOrEmpty(status))
        {
            query += $"&status={Uri.EscapeDataString(status)}";
        }
        if (!string.IsNullOrEmpty(referenceId))
        {
            query += $"&referenceId={Uri.EscapeDataString(referenceId)}";
        }
        if (after.HasValue)
        {
            query += $"&after={after.Value:D}";
        }
        if (before.HasValue)
        {
            query += $"&before={before.Value:D}";
        }

        var result = await _httpClient.GetFromJsonAsync<IReadOnlyList<ClientModels.MessagePayload>>(query, cancellationToken).ConfigureAwait(false);
        return result ?? [];
    }

    /// <summary>Sends a message into a conversation thread.</summary>
    /// <param name="referenceId">The thread this message belongs to.</param>
    /// <param name="recipientUserId">The user the message is addressed to.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="body">The message body.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored message.</returns>
    /// <remarks>
    /// The thread id is a required argument rather than an optional one the client would fill in.
    /// A client that minted one when it was not given would open a new conversation every time a
    /// caller forgot it, and the other participant would never see the turn.
    ///
    /// Which side is speaking is not sent. The server derives it from how this client authenticated,
    /// so a caller cannot post as the other party.
    /// </remarks>
    public virtual async Task<ClientModels.MessagePayload?> SendMessage(
        string referenceId,
        Guid recipientUserId,
        string subject,
        string? body,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "messages",
            new
            {
                ReferenceId = referenceId,
                RecipientUserId = recipientUserId,
                Subject = subject,
                Body = body,
            },
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ClientModels.MessagePayload>(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Gets a single message by identifier.</summary>
    /// <returns>The message, or <c>null</c> if not found.</returns>
    public virtual Task<ClientModels.MessagePayload?> GetMessage(Guid id, CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<ClientModels.MessagePayload>($"messages/{id}", cancellationToken);

    /// <summary>Gets the count of unread messages for the current user.</summary>
    /// <returns>The unread message count.</returns>
    public virtual async Task<int> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<UnreadCountResponse>("messages/unread-count", cancellationToken).ConfigureAwait(false);
        return response?.Count ?? 0;
    }


    /// <summary>Marks a single message as read.</summary>
    public virtual Task MarkRead(Guid id, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"messages/{id}/read", null, cancellationToken);

    /// <summary>Marks all of the current user's messages as read.</summary>
    public virtual Task MarkAllRead(CancellationToken cancellationToken = default)
        => _httpClient.PostAsync("messages/mark-all-read", null, cancellationToken);

    /// <summary>Dismisses a single message.</summary>
    public virtual Task Dismiss(Guid id, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"messages/{id}/dismiss", null, cancellationToken);

    /// <summary>Archives a single message.</summary>
    public virtual Task Archive(Guid id, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"messages/{id}/archive", null, cancellationToken);

    /// <summary>Gets the current user's access requests.</summary>
    /// <returns>The access requests.</returns>
    public virtual async Task<IReadOnlyList<ClientModels.AccessRequestPayload>> GetAccessRequests(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<IReadOnlyList<ClientModels.AccessRequestPayload>>("access-requests", cancellationToken).ConfigureAwait(false);
        return result ?? [];
    }

    /// <summary>Creates a new access request.</summary>
    /// <returns>The created access request, or <c>null</c> if the response carried no body.</returns>
    public virtual async Task<ClientModels.AccessRequestPayload?> CreateAccessRequest(ClientModels.CreateAccessRequestModel model, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("access-requests", model, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClientModels.AccessRequestPayload>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Approves an access request, optionally with reviewer notes.</summary>
    public virtual Task ApproveAccessRequest(Guid id, string? notes = null, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync($"access-requests/{id}/approve", new { Notes = notes }, cancellationToken);

    /// <summary>Denies an access request, optionally with reviewer notes.</summary>
    public virtual Task DenyAccessRequest(Guid id, string? notes = null, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync($"access-requests/{id}/deny", new { Notes = notes }, cancellationToken);

    private sealed class UnreadCountResponse
    {
        public int Count { get; set; }
    }
}
