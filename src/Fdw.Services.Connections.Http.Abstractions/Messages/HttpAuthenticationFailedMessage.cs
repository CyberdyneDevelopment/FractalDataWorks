using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Http.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that HTTP authentication failed.
/// </summary>
[Message("HttpAuthenticationFailed")]
[MessageOption(typeof(HttpMessageCollectionBase))]
public sealed class HttpAuthenticationFailedMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpAuthenticationFailedMessage"/> class.
    /// </summary>
    public HttpAuthenticationFailedMessage()
        : base(4001, "HttpAuthenticationFailed", MessageSeverity.Error,
               "HTTP authentication failed",
               "HTTP_AUTH_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with authentication type.
    /// </summary>
    /// <param name="authenticationType">The authentication type that failed.</param>
    public HttpAuthenticationFailedMessage(string authenticationType)
        : base(4001, "HttpAuthenticationFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP authentication failed for type: {0}", authenticationType),
               "HTTP_AUTH_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with authentication type and reason.
    /// </summary>
    /// <param name="authenticationType">The authentication type that failed.</param>
    /// <param name="reason">The reason for the authentication failure.</param>
    public HttpAuthenticationFailedMessage(string authenticationType, string reason)
        : base(4001, "HttpAuthenticationFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP authentication failed for type: {0}, reason: {1}", authenticationType, reason),
               "HTTP_AUTH_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with detailed authentication context.
    /// </summary>
    /// <param name="authenticationType">The authentication type that failed.</param>
    /// <param name="reason">The reason for the authentication failure.</param>
    /// <param name="endpoint">The endpoint that failed authentication.</param>
    public HttpAuthenticationFailedMessage(string authenticationType, string reason, string endpoint)
        : base(4001, "HttpAuthenticationFailed", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP authentication failed for type: {0}, reason: {1}, endpoint: {2}", authenticationType, reason, endpoint),
               "HTTP_AUTH_FAILED")
    { }
}