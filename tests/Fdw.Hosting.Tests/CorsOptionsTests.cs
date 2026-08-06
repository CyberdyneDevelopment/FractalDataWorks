using System;
using System.Linq;
using Fdw.Hosting.Configuration;
using Xunit;
using Shouldly;

namespace Fdw.Hosting.Tests;

public class CorsOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SectionNameIsCors()
    {
        CorsOptions.SectionName.ShouldBe("Cors");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EnabledDefaultsToTrue()
    {
        var options = new CorsOptions();
        options.Enabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OriginsDefaultsToEmpty()
    {
        var options = new CorsOptions();
        options.Origins.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MethodsHasDefaultValues()
    {
        var options = new CorsOptions();

        options.Methods.ShouldContain("GET");
        options.Methods.ShouldContain("POST");
        options.Methods.ShouldContain("PUT");
        options.Methods.ShouldContain("DELETE");
        options.Methods.ShouldContain("PATCH");
        options.Methods.ShouldContain("OPTIONS");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HeadersHasDefaultValues()
    {
        var options = new CorsOptions();

        options.Headers.ShouldContain("Content-Type");
        options.Headers.ShouldContain("Authorization");
        options.Headers.ShouldContain("X-Tenant-Id");
        options.Headers.ShouldContain("X-Correlation-Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ExposedHeadersHasDefaultValues()
    {
        var options = new CorsOptions();

        options.ExposedHeaders.ShouldContain("X-Correlation-Id");
        options.ExposedHeaders.ShouldContain("X-RateLimit-Limit");
        options.ExposedHeaders.ShouldContain("X-RateLimit-Remaining");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllowCredentialsDefaultsToTrue()
    {
        var options = new CorsOptions();
        options.AllowCredentials.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PreflightMaxAgeSecondsDefaultsTo600()
    {
        var options = new CorsOptions();
        options.PreflightMaxAgeSeconds.ShouldBe(600);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OriginsCanBeSet()
    {
        var options = new CorsOptions();
        options.Origins = ["https://example.com", "https://app.example.com"];

        options.Origins.Count.ShouldBe(2);
        options.Origins.First().ShouldBe("https://example.com");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PreflightMaxAgeSecondsCanBeSet()
    {
        var options = new CorsOptions { PreflightMaxAgeSeconds = 1200 };
        options.PreflightMaxAgeSeconds.ShouldBe(1200);
    }
}
