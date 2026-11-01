using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Certificate name is required for the operation.
/// </summary>
[TypeOption(typeof(SecretManagerResultCodes), "CertificateNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CertificateNameRequiredCode : SecretManagerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateNameRequiredCode"/> class.
    /// </summary>
    public CertificateNameRequiredCode()
        : base(21000, "CertificateNameRequired",
            ResultSeverities.ByName("Error"),
            "Certificate name is required for {Operation} operation",
            isRetryable: false)
    {
    }
}
