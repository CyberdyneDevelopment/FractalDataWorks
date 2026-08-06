using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to parse SOAP response.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SoapResponseParseFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SoapResponseParseFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapResponseParseFailedCode"/> class.
    /// </summary>
    public SoapResponseParseFailedCode()
        : base(91009, "SoapResponseParseFailed",
            ResultSeverities.ByName("Error"),
            "Failed to parse SOAP response: {ErrorMessage}",
            isRetryable: false)
    {
    }
}