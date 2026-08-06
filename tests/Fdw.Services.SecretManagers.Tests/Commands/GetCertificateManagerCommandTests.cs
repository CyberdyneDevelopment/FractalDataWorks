using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Commands;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Tests.Commands;

public class GetCertificateManagerCommandTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsProperties()
    {
        var cmd = new GetCertificateManagerCommand("vault", "myCert");

        cmd.Container.ShouldBe("vault");
        cmd.SecretKey.ShouldBe("myCert");
        cmd.CommandType.ShouldBe("GetCertificate");
        cmd.IsSecretModifying.ShouldBeFalse();
        cmd.ExpectedResultType.ShouldBe(typeof(SecretValue));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithEmptyCertificateNameThrows()
    {
        Should.Throw<ArgumentException>(() => new GetCertificateManagerCommand("vault", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithWhitespaceCertificateNameThrows()
    {
        Should.Throw<ArgumentException>(() => new GetCertificateManagerCommand("vault", "   "));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void LatestCreatesCommandWithPrivateKey()
    {
        var cmd = GetCertificateManagerCommand.Latest("vault", "cert");

        cmd.SecretKey.ShouldBe("cert");
        cmd.IncludePrivateKey.ShouldBeTrue();
        cmd.Version.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void LatestWithoutPrivateKeyCreatesCommand()
    {
        var cmd = GetCertificateManagerCommand.Latest("vault", "cert", includePrivateKey: false);

        cmd.IncludePrivateKey.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ForVersionCreatesCommandWithVersion()
    {
        var cmd = GetCertificateManagerCommand.ForVersion("vault", "cert", "v2");

        cmd.Version.ShouldBe("v2");
        cmd.IncludePrivateKey.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ForVersionWithEmptyVersionThrows()
    {
        Should.Throw<ArgumentException>(() => GetCertificateManagerCommand.ForVersion("vault", "cert", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ForVersionWithoutPrivateKeyCreatesCommand()
    {
        var cmd = GetCertificateManagerCommand.ForVersion("vault", "cert", "v1", includePrivateKey: false);

        cmd.Version.ShouldBe("v1");
        cmd.IncludePrivateKey.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersCreatesNewCommand()
    {
        var cmd = GetCertificateManagerCommand.Latest("vault", "cert");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataCreatesNewCommand()
    {
        var cmd = GetCertificateManagerCommand.Latest("vault", "cert");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsForValidCommand()
    {
        var cmd = new GetCertificateManagerCommand("vault", "cert");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TimeoutIsPassedThrough()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var cmd = GetCertificateManagerCommand.Latest("vault", "cert", timeout: timeout);

        cmd.Timeout.ShouldBe(timeout);
    }
}
