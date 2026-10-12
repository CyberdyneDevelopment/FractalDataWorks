using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// What a Session entry has to carry before a scheme is taken for it (FDW-903). Everything a request
/// will need is checked here, so a misconfigured service stops the host at startup.
/// </summary>
public sealed class SessionTakeSchemeTests
{
    [Fact]
    public void Takes_a_cookie_bound_scheme_for_a_complete_origin_checked_entry()
    {
        var schemes = new RecordingSchemeProvider();

        var result = new SessionAuthenticationType().TakeScheme(Entry(), schemes, Services(), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SchemeName.ShouldBe("Fdw.Session.ConsoleSession");
        result.Value.Issuer.ShouldBe(SessionAuthenticationType.SessionIssuer);
        result.Value.CookieName.ShouldBe("fdw_session");
        schemes.Added.ShouldContain("Fdw.Session.ConsoleSession");
    }

    [Fact]
    public void Takes_a_scheme_for_a_complete_synchronizer_token_entry()
    {
        var entry = Entry();
        entry.CsrfProtection = "SynchronizerToken";
        entry.AllowedOrigins = null;
        entry.CsrfHeaderName = "X-CSRF-Token";

        new SessionAuthenticationType().TakeScheme(entry, new RecordingSchemeProvider(), Services(), null)
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Needs_no_authority()
        => new SessionAuthenticationType().TakeScheme(Entry(), new RecordingSchemeProvider(), Services(), null)
            .IsSuccess.ShouldBeTrue();

    [Fact]
    public void Refuses_an_entry_with_no_cookie_name()
    {
        var entry = Entry();
        entry.CookieName = string.Empty;

        Refused(entry);
    }

    [Theory]
    [InlineData("strict")]
    [InlineData("Unspecified")]
    [InlineData("")]
    public void Refuses_a_same_site_mode_that_is_not_exactly_strict_lax_or_none(string sameSite)
    {
        var entry = Entry();
        entry.SameSite = sameSite;

        Refused(entry);
    }

    [Fact]
    public void Refuses_an_unregistered_csrf_protection()
    {
        var entry = Entry();
        entry.CsrfProtection = "DoubleSubmit";

        Refused(entry);
    }

    [Fact]
    public void Refuses_origin_check_with_no_allowed_origins()
    {
        var entry = Entry();
        entry.AllowedOrigins = null;

        Refused(entry);
    }

    [Fact]
    public void Refuses_origin_check_with_an_allowed_origin_that_carries_a_path()
    {
        var entry = Entry();
        entry.AllowedOrigins = "https://console.example/app";

        Refused(entry);
    }

    [Fact]
    public void Refuses_origin_check_that_also_names_a_csrf_header()
    {
        var entry = Entry();
        entry.CsrfHeaderName = "X-CSRF-Token";

        Refused(entry);
    }

    [Fact]
    public void Refuses_a_synchronizer_token_entry_with_no_header()
    {
        var entry = Entry();
        entry.CsrfProtection = "SynchronizerToken";
        entry.AllowedOrigins = null;

        Refused(entry);
    }

    private static void Refused(SessionAuthenticationConfiguration entry)
    {
        var schemes = new RecordingSchemeProvider();

        new SessionAuthenticationType().TakeScheme(entry, schemes, Services(), null).IsSuccess.ShouldBeFalse();
        schemes.Added.ShouldBeEmpty();
    }

    private static SessionAuthenticationConfiguration Entry()
        => new()
        {
            Name = "ConsoleSession",
            Implementation = "Session",
            Enabled = true,
            CookieName = "fdw_session",
            SameSite = "Strict",
            CsrfProtection = "OriginCheck",
            AllowedOrigins = "https://console.example https://console.example:8443",
        };

    private static ServiceProvider Services() => new ServiceCollection().BuildServiceProvider();

    private sealed class RecordingSchemeProvider : IAuthenticationSchemeProvider
    {
        public List<string> Added { get; } = [];

        public void AddScheme(AuthenticationScheme scheme) => Added.Add(scheme.Name);

        public void RemoveScheme(string name) => Added.Remove(name);

        public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
            => Task.FromResult<IEnumerable<AuthenticationScheme>>([]);

        public Task<AuthenticationScheme?> GetSchemeAsync(string name)
            => Task.FromResult<AuthenticationScheme?>(null);

        public Task<AuthenticationScheme?> GetDefaultAuthenticateSchemeAsync()
            => Task.FromResult<AuthenticationScheme?>(null);

        public Task<AuthenticationScheme?> GetDefaultChallengeSchemeAsync()
            => Task.FromResult<AuthenticationScheme?>(null);

        public Task<AuthenticationScheme?> GetDefaultForbidSchemeAsync()
            => Task.FromResult<AuthenticationScheme?>(null);

        public Task<AuthenticationScheme?> GetDefaultSignInSchemeAsync()
            => Task.FromResult<AuthenticationScheme?>(null);

        public Task<AuthenticationScheme?> GetDefaultSignOutSchemeAsync()
            => Task.FromResult<AuthenticationScheme?>(null);

        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
            => Task.FromResult<IEnumerable<AuthenticationScheme>>([]);
    }
}
