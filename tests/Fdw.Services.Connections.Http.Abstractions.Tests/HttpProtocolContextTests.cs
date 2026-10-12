using Fdw.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fdw.Services.Connections.Http.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

public class HttpProtocolContextTests
{
    private readonly Mock<IGenericConfiguration> _mockConfiguration;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;

    public HttpProtocolContextTests()
    {
        _mockConfiguration = new Mock<IGenericConfiguration>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsConfiguration()
    {
        // Arrange & Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            null,
            null);

        // Assert
        context.Configuration.ShouldBe(_mockConfiguration.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsLoggerFactory()
    {
        // Arrange & Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            null,
            null);

        // Assert
        context.LoggerFactory.ShouldBe(_mockLoggerFactory.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsResolvedCertificateToNull()
    {
        // Arrange & Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            null,
            null);

        // Assert
        context.ResolvedCertificate.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsResolvedCertificate()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=TestCert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        // Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            certificate,
            null,
            null);

        // Assert
        context.ResolvedCertificate.ShouldBe(certificate);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsResolvedPasswordToNull()
    {
        // Arrange & Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            null,
            null);

        // Assert
        context.ResolvedPassword.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsResolvedPassword()
    {
        // Arrange
        const string password = "TestPassword";

        // Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            password,
            null);

        // Assert
        context.ResolvedPassword.ShouldBe(password);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsResolvedApiKeyToNull()
    {
        // Arrange & Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            null,
            null);

        // Assert
        context.ResolvedApiKey.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsResolvedApiKey()
    {
        // Arrange
        const string apiKey = "TestApiKey";

        // Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            null,
            apiKey);

        // Assert
        context.ResolvedApiKey.ShouldBe(apiKey);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsAllProperties()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=TestCert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        const string password = "TestPassword";
        const string apiKey = "TestApiKey";

        // Act
        var context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            certificate,
            password,
            apiKey);

        // Assert
        context.Configuration.ShouldBe(_mockConfiguration.Object);
        context.LoggerFactory.ShouldBe(_mockLoggerFactory.Object);
        context.ResolvedCertificate.ShouldBe(certificate);
        context.ResolvedPassword.ShouldBe(password);
        context.ResolvedApiKey.ShouldBe(apiKey);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RecordSupportsEquality()
    {
        // Arrange
        var context1 = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            "password",
            "apiKey");

        var context2 = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            "password",
            "apiKey");

        // Act & Assert
        context1.Equals(context2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RecordSupportsInequality()
    {
        // Arrange
        var context1 = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            "password1",
            "apiKey");

        var context2 = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            "password2",
            "apiKey");

        // Act & Assert
        context1.Equals(context2).ShouldBeFalse();
    }
}
