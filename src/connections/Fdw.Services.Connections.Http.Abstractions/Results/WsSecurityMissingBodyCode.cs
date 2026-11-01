using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// WS-Security envelope missing Body element.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "WsSecurityMissingBody", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WsSecurityMissingBodyCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WsSecurityMissingBodyCode"/> class.
    /// </summary>
    public WsSecurityMissingBodyCode()
        : base(91011, "WsSecurityMissingBody",
            ResultSeverities.ByName("Error"),
            "SOAP envelope missing Body element",
            isRetryable: false)
    {
    }
}