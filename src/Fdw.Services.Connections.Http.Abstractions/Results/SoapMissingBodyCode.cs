using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// SOAP response missing Body element.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SoapMissingBody", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SoapMissingBodyCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapMissingBodyCode"/> class.
    /// </summary>
    public SoapMissingBodyCode()
        : base(91007, "SoapMissingBody",
            ResultSeverities.ByName("Error"),
            "SOAP response missing Body element",
            isRetryable: false)
    {
    }
}