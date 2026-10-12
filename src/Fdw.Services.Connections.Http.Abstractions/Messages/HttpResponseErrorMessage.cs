using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Http.Abstractions.Messages;

/// <summary>
/// HTTP response error message.
/// </summary>
[Message("HttpResponseError")]
[MessageOption(typeof(HttpMessageCollectionBase))]
public sealed class HttpResponseErrorMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResponseErrorMessage"/> class.
    /// </summary>
    public HttpResponseErrorMessage()
        : base(4003, "HttpResponseError", MessageSeverity.Error,
               "HTTP response error", "HTTP_RESPONSE_ERROR")
    { }

    /// <summary>
    /// Initializes a new instance with status code details.
    /// </summary>
    /// <param name="statusCode">The HTTP status code received.</param>
    /// <param name="reasonPhrase">The reason phrase from the response.</param>
    public HttpResponseErrorMessage(int statusCode, string? reasonPhrase = null)
        : base(4003, "HttpResponseError", MessageSeverity.Error,
               $"HTTP response error: {statusCode} {reasonPhrase ?? ""}".Trim(), "HTTP_RESPONSE_ERROR")
    { }
}