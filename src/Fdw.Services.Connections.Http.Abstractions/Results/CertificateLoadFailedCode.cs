using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to load certificate.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "CertificateLoadFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CertificateLoadFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateLoadFailedCode"/> class.
    /// </summary>
    public CertificateLoadFailedCode()
        : base(60004, "CertificateLoadFailed",
            ResultSeverities.ByName("Error"),
            "Failed to load certificate: {ErrorMessage}",
            isRetryable: false)
    {
    }
}