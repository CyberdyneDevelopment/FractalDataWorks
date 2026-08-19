using System.Collections.Generic;
using Fdw.Services.Authentication.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// Tests for reading the host's <c>AuthenticationServices</c> declarations — the section that says
/// which issuers this host trusts and which mechanism validates each.
/// </summary>
public sealed class AuthenticationServiceConfigurationTests
{
    [Fact]
    public void Read_selects_only_the_entries_naming_the_asked_for_mechanism()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(
            ("0:Name", "FdwAuthority"), ("0:ServiceOptionType", "OpenIddict"),
            ("0:Enabled", "true"), ("0:Authority", "https://auth.example/"),
            ("1:Name", "Partner"), ("1:ServiceOptionType", "JwtBearer"),
            ("1:Enabled", "true"), ("1:Authority", "https://idp.example/o/partner/")),
            "JwtBearer", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().Header.Name.ShouldBe("Partner");
    }

    [Fact]
    public void Read_skips_a_declared_but_disabled_entry()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(
            ("0:Name", "Partner"), ("0:ServiceOptionType", "JwtBearer"),
            ("0:Enabled", "false"), ("0:Authority", "https://idp.example/o/partner/")),
            "JwtBearer", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public void Read_reports_no_entries_when_the_host_declares_none_for_this_mechanism()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(), "JwtBearer", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    // The trailing slash is the whole point: OpenIddict stamps https://host/ on a token for an
    // authority written https://host, and a verbatim match would miss every one of them.
    [Fact]
    public void Read_normalises_the_authority_to_the_form_an_issuer_puts_in_the_claim()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(
            ("0:Name", "FdwAuthority"), ("0:ServiceOptionType", "OpenIddict"),
            ("0:Enabled", "true"), ("0:Authority", "https://internal-proj.example")),
            "OpenIddict", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().Header.Authority.ShouldBe("https://internal-proj.example/");
    }

    [Fact]
    public void Read_fails_on_an_entry_that_names_no_mechanism()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(
            ("0:Name", "Nameless"), ("0:Enabled", "true"), ("0:Authority", "https://idp.example/")),
            "JwtBearer", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Read_fails_on_an_enabled_entry_with_no_authority()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(
            ("0:Name", "Partner"), ("0:ServiceOptionType", "JwtBearer"), ("0:Enabled", "true")),
            "JwtBearer", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Read_fails_on_an_authority_that_is_not_an_absolute_uri()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(
            ("0:Name", "Partner"), ("0:ServiceOptionType", "JwtBearer"),
            ("0:Enabled", "true"), ("0:Authority", "/connect")),
            "JwtBearer", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Read_fails_on_an_enabled_entry_with_no_name()
    {
        var result = AuthenticationServiceConfiguration.Read(Config(
            ("0:ServiceOptionType", "JwtBearer"), ("0:Enabled", "true"),
            ("0:Authority", "https://idp.example/")),
            "JwtBearer", NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
    }

    private static IConfiguration Config(params (string Key, string Value)[] entries)
    {
        var values = new Dictionary<string, string?>(System.StringComparer.Ordinal);
        foreach (var (key, value) in entries)
            values[AuthenticationServiceConfiguration.SectionName + ":" + key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
