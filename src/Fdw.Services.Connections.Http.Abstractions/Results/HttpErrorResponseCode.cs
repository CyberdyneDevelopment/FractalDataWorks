using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// HTTP error response received.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "HttpErrorResponse", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HttpErrorResponseCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpErrorResponseCode"/> class.
    /// </summary>
    public HttpErrorResponseCode()
        : base(71003, "HttpErrorResponse",
            ResultSeverities.ByName("Error"),
            "HTTP {StatusCode}: {ReasonPhrase}. {ErrorContent}",
            isRetryable: true)
    {
    }
}