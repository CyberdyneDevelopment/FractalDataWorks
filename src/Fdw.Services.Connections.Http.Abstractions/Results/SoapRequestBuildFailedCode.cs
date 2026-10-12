using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to build SOAP request.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SoapRequestBuildFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SoapRequestBuildFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapRequestBuildFailedCode"/> class.
    /// </summary>
    public SoapRequestBuildFailedCode()
        : base(91008, "SoapRequestBuildFailed",
            ResultSeverities.ByName("Error"),
            "Failed to build SOAP request: {ErrorMessage}",
            isRetryable: false)
    {
    }
}