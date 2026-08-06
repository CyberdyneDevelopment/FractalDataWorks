using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// HTTP request was cancelled.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "RequestCancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequestCancelledCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestCancelledCode"/> class.
    /// </summary>
    public RequestCancelledCode()
        : base(10010, "RequestCancelled",
            ResultSeverities.ByName("Warning"),
            "Request was cancelled",
            isRetryable: false)
    {
    }
}