using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Http.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that HTTP request timed out.
/// </summary>
[Message("HttpRequestTimeout")]
[MessageOption(typeof(HttpMessageCollectionBase))]
public sealed class HttpRequestTimeoutMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestTimeoutMessage"/> class.
    /// </summary>
    public HttpRequestTimeoutMessage()
        : base(4101, "HttpRequestTimeout", MessageSeverity.Warning,
               "HTTP request timed out",
               "HTTP_REQUEST_TIMEOUT")
    { }

    /// <summary>
    /// Initializes a new instance with timeout duration.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout duration in seconds.</param>
    public HttpRequestTimeoutMessage(int timeoutSeconds)
        : base(4101, "HttpRequestTimeout", MessageSeverity.Warning,
               string.Format(CultureInfo.InvariantCulture, "HTTP request timed out after {0} seconds", timeoutSeconds),
               "HTTP_REQUEST_TIMEOUT")
    { }

    /// <summary>
    /// Initializes a new instance with timeout and endpoint.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout duration in seconds.</param>
    /// <param name="endpoint">The endpoint that timed out.</param>
    public HttpRequestTimeoutMessage(int timeoutSeconds, string endpoint)
        : base(4101, "HttpRequestTimeout", MessageSeverity.Warning,
               string.Format(CultureInfo.InvariantCulture, "HTTP request timed out after {0} seconds for endpoint: {1}", timeoutSeconds, endpoint),
               "HTTP_REQUEST_TIMEOUT")
    { }

    /// <summary>
    /// Initializes a new instance with full request context.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout duration in seconds.</param>
    /// <param name="endpoint">The endpoint that timed out.</param>
    /// <param name="httpMethod">The HTTP method used.</param>
    public HttpRequestTimeoutMessage(int timeoutSeconds, string endpoint, string httpMethod)
        : base(4101, "HttpRequestTimeout", MessageSeverity.Warning,
               string.Format(CultureInfo.InvariantCulture, "HTTP {0} request timed out after {1} seconds for endpoint: {2}", httpMethod, timeoutSeconds, endpoint),
               "HTTP_REQUEST_TIMEOUT")
    { }
}