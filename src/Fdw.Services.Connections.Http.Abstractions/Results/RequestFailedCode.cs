using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// HTTP request failed with an exception.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "RequestFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequestFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestFailedCode"/> class.
    /// </summary>
    public RequestFailedCode()
        : base(70000, "RequestFailed",
            ResultSeverities.ByName("Error"),
            "HTTP request failed: {ErrorMessage}",
            isRetryable: true)
    {
    }
}