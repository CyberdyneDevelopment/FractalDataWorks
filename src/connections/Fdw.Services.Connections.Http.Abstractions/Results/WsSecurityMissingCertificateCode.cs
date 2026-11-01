using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// WS-Security requires a certificate but none was provided.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "WsSecurityMissingCertificate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WsSecurityMissingCertificateCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WsSecurityMissingCertificateCode"/> class.
    /// </summary>
    public WsSecurityMissingCertificateCode()
        : base(60000, "WsSecurityMissingCertificate",
            ResultSeverities.ByName("Error"),
            "WS-Security requires a certificate but none was resolved",
            isRetryable: false)
    {
    }
}