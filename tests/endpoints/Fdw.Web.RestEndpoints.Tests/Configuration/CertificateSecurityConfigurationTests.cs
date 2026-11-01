using System.Security.Cryptography.X509Certificates;
using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class CertificateSecurityConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new CertificateSecurityConfiguration();

        // Assert
        config.Enabled.ShouldBeFalse();
        config.RequireClientCertificate.ShouldBeFalse();
        config.RevocationMode.ShouldBe(X509RevocationMode.Online);
        config.VerificationFlags.ShouldBe(X509VerificationFlags.NoFlag);
        config.TrustedCertificateAuthorities.ShouldNotBeNull();
        config.TrustedCertificateAuthorities.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new CertificateSecurityConfiguration
        {
            Enabled = true,
            RequireClientCertificate = true,
            RevocationMode = X509RevocationMode.Offline,
            VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority,
            TrustedCertificateAuthorities = ["CA1", "CA2"]
        };

        // Assert
        config.Enabled.ShouldBeTrue();
        config.RequireClientCertificate.ShouldBeTrue();
        config.RevocationMode.ShouldBe(X509RevocationMode.Offline);
        config.VerificationFlags.ShouldBe(X509VerificationFlags.AllowUnknownCertificateAuthority);
        config.TrustedCertificateAuthorities.ShouldBe(new[] { "CA1", "CA2" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Enabled_CanBeSet()
    {
        // Arrange & Act
        var config = new CertificateSecurityConfiguration { Enabled = true };

        // Assert
        config.Enabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequireClientCertificate_CanBeSet()
    {
        // Arrange & Act
        var config = new CertificateSecurityConfiguration { RequireClientCertificate = true };

        // Assert
        config.RequireClientCertificate.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RevocationMode_CanBeSet()
    {
        // Arrange & Act
        var config = new CertificateSecurityConfiguration { RevocationMode = X509RevocationMode.NoCheck };

        // Assert
        config.RevocationMode.ShouldBe(X509RevocationMode.NoCheck);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void VerificationFlags_CanBeSet()
    {
        // Arrange & Act
        var config = new CertificateSecurityConfiguration
        {
            VerificationFlags = X509VerificationFlags.IgnoreWrongUsage
        };

        // Assert
        config.VerificationFlags.ShouldBe(X509VerificationFlags.IgnoreWrongUsage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TrustedCertificateAuthorities_CanBeSet()
    {
        // Arrange
        var authorities = new[] { "Verisign", "DigiCert", "LetsEncrypt" };

        // Act
        var config = new CertificateSecurityConfiguration { TrustedCertificateAuthorities = authorities };

        // Assert
        config.TrustedCertificateAuthorities.ShouldBe(authorities);
    }
}
