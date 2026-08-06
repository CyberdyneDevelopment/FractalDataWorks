using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Failed to export certificate.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "CertificateExportFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CertificateExportFailedCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateExportFailedCode"/> class.
    /// </summary>
    public CertificateExportFailedCode()
        : base(90001, "CertificateExportFailed",
            ResultSeverities.ByName("Error"),
            "Failed to export certificate '{CertificateName}': {ErrorMessage}",
            isRetryable: false)
    {
    }
}
