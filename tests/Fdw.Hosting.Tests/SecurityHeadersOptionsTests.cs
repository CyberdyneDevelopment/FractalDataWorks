using System;
using Fdw.Hosting.Configuration;
using Xunit;
using Shouldly;

namespace Fdw.Hosting.Tests;

public class SecurityHeadersOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllowFramingDefaultsToFalse()
    {
        var options = new SecurityHeadersOptions();
        options.AllowFraming.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ContentSecurityPolicyDefaultsToNull()
    {
        var options = new SecurityHeadersOptions();
        options.ContentSecurityPolicy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EnableDefaultCspDefaultsToTrue()
    {
        var options = new SecurityHeadersOptions();
        options.EnableDefaultCsp.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SensitivePathsHasDefaultValues()
    {
        var options = new SecurityHeadersOptions();

        options.SensitivePaths.Length.ShouldBe(3);
        options.SensitivePaths.ShouldContain("/api/v1/auth");
        options.SensitivePaths.ShouldContain("/api/v1/users");
        options.SensitivePaths.ShouldContain("/api/v1/tenants");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllowFramingCanBeSet()
    {
        var options = new SecurityHeadersOptions { AllowFraming = true };
        options.AllowFraming.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ContentSecurityPolicyCanBeSet()
    {
        var options = new SecurityHeadersOptions { ContentSecurityPolicy = "default-src 'none'" };
        options.ContentSecurityPolicy.ShouldBe("default-src 'none'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SensitivePathsCanBeOverridden()
    {
        var options = new SecurityHeadersOptions { SensitivePaths = ["/custom/path"] };
        options.SensitivePaths.Length.ShouldBe(1);
        options.SensitivePaths[0].ShouldBe("/custom/path");
    }
}
