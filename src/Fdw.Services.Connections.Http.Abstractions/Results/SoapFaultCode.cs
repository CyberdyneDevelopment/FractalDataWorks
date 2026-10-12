using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// SOAP Fault received.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SoapFault", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SoapFaultCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapFaultCode"/> class.
    /// </summary>
    public SoapFaultCode()
        : base(71004, "SoapFault",
            ResultSeverities.ByName("Error"),
            "SOAP Fault [{FaultCode}]: {FaultString}",
            isRetryable: false)
    {
    }
}