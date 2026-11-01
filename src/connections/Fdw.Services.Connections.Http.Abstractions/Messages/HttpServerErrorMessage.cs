using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Http.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that HTTP server returned an error (5xx).
/// </summary>
[Message("HttpServerError")]
[MessageOption(typeof(HttpMessageCollectionBase))]
public sealed class HttpServerErrorMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServerErrorMessage"/> class.
    /// </summary>
    public HttpServerErrorMessage()
        : base(4201, "HttpServerError", MessageSeverity.Error,
               "HTTP server error (5xx)",
               "HTTP_SERVER_ERROR")
    { }

    /// <summary>
    /// Initializes a new instance with status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned.</param>
    public HttpServerErrorMessage(int statusCode)
        : base(4201, "HttpServerError", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP server error ({0})", statusCode),
               "HTTP_SERVER_ERROR")
    { }

    /// <summary>
    /// Initializes a new instance with status code and endpoint.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned.</param>
    /// <param name="endpoint">The endpoint that returned the error.</param>
    public HttpServerErrorMessage(int statusCode, string endpoint)
        : base(4201, "HttpServerError", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP server error ({0}) for endpoint: {1}", statusCode, endpoint),
               "HTTP_SERVER_ERROR")
    { }

    /// <summary>
    /// Initializes a new instance with full error context.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned.</param>
    /// <param name="endpoint">The endpoint that returned the error.</param>
    /// <param name="errorMessage">The error message from the server.</param>
    public HttpServerErrorMessage(int statusCode, string endpoint, string errorMessage)
        : base(4201, "HttpServerError", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "HTTP server error ({0}) for endpoint: {1}, message: {2}", statusCode, endpoint, errorMessage),
               "HTTP_SERVER_ERROR")
    { }
}