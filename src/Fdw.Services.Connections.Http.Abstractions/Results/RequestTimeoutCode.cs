using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// HTTP request timed out.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "RequestTimeout", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequestTimeoutCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutCode"/> class.
    /// </summary>
    public RequestTimeoutCode()
        : base(80000, "RequestTimeout",
            ResultSeverities.ByName("Error"),
            "Request timed out",
            isRetryable: true)
    {
    }
}