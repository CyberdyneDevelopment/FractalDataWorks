using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.RowSources.Http.Abstractions.Results;

/// <summary>
/// HTTP request failed with non-success status code.
/// </summary>
[TypeOption(typeof(HttpRowSourceResultCodes), "HttpRequestFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HttpRequestFailedCode : HttpRowSourceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestFailedCode"/> class.
    /// </summary>
    public HttpRequestFailedCode()
        : base(70000, "HttpRequestFailed",
            ResultSeverities.ByName("Error"),
            "HTTP {StatusCode}: {ReasonPhrase}",
            isRetryable: true)
    {
    }
}