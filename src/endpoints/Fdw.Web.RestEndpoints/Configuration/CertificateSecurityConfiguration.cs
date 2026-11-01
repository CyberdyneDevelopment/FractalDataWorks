using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// Certificate-based security configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CertificateSecurityConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether certificate authentication is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether client certificates are required.
    /// </summary>
    public bool RequireClientCertificate { get; init; }

    /// <summary>
    /// Gets or sets the validation mode for client certificates.
    /// </summary>
    public X509RevocationMode RevocationMode { get; init; } = X509RevocationMode.Online;

    /// <summary>
    /// Gets or sets the certificate validation flags.
    /// </summary>
    public X509VerificationFlags VerificationFlags { get; init; } = X509VerificationFlags.NoFlag;

    /// <summary>
    /// Gets or sets the trusted certificate authorities.
    /// </summary>
    public string[] TrustedCertificateAuthorities { get; init; } = [];
}