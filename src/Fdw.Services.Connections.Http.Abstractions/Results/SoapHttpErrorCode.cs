using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// SOAP HTTP error response.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SoapHttpError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SoapHttpErrorCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapHttpErrorCode"/> class.
    /// </summary>
    public SoapHttpErrorCode()
        : base(71005, "SoapHttpError",
            ResultSeverities.ByName("Error"),
            "HTTP {StatusCode}: {ReasonPhrase}",
            isRetryable: true)
    {
    }
}