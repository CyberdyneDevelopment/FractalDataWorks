using System;
using System.Collections.Generic;
using Fdw.Services.Authentication.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// Tests for reading a <c>LocalKey</c> entry — the mechanism that validates tokens this host issued
/// itself.
/// </summary>
public sealed class LocalKeyAuthenticationConfigurationTests
{
    [Fact]
    public void Read_accepts_an_entry_declaring_only_an_audience()
    {
        var result = LocalKeyAuthenticationConfiguration.Read(
            Section(("Audience", "reference-api")), "FdwAuthority", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Audience.ShouldBe("reference-api");
    }

    [Fact]
    public void Read_does_not_require_roles()
    {
        // The distinction this whole type exists for. JwtBearer requires Roles because a remote
        // issuer's token says nothing about what the caller may do here. A LocalKey token was minted
        // by this host's own flow with that principal's roles and permissions already baked in, so
        // requiring roles against the issuer would confer one set on every user the host signs in.
        var result = LocalKeyAuthenticationConfiguration.Read(
            Section(("Audience", "reference-api")), "FdwAuthority", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();

        LocalKeyAuthenticationConfiguration.Read(
                Section(("Audience", "reference-api"), ("Roles", "Admin")),
                "FdwAuthority", NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Read_refuses_an_entry_with_no_usable_audience(string? audience)
    {
        // Refused rather than defaulted: a token is accepted only for the audience it names, so a
        // guessed one rejects every token this host issued and looks like a signing failure.
        LocalKeyAuthenticationConfiguration.Read(
                Section(("Audience", audience)), "FdwAuthority", NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Read_rejects_a_null_section()
    {
        Should.Throw<ArgumentNullException>(() =>
            LocalKeyAuthenticationConfiguration.Read(null!, "FdwAuthority", NullLogger.Instance));
    }

    private static IConfigurationSection Section(params (string Key, string? Value)[] entries)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in entries)
            values["Entry:" + key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Entry");
    }
}
