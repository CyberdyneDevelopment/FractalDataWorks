using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// What a LocalKey entry has to carry before a scheme is taken for it.
/// </summary>
public sealed class LocalKeyTakeSchemeTests
{
    [Fact]
    public void Takes_a_scheme_for_a_complete_entry()
    {
        var schemes = new RecordingSchemeProvider();

        var result = new LocalKeyAuthenticationType().TakeScheme(
            Entry("FdwAuthority", "https://auth.example/"), schemes, Services(), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SchemeName.ShouldBe("Fdw.LocalKey.FdwAuthority");
        result.Value.Issuer.ShouldBe("https://auth.example/");
        schemes.Added.ShouldContain("Fdw.LocalKey.FdwAuthority");
    }

    [Fact]
    public void Refuses_an_entry_with_no_name()
    {
        var schemes = new RecordingSchemeProvider();

        var result = new LocalKeyAuthenticationType().TakeScheme(
            Entry(null, "https://auth.example/"), schemes, Services(), null);

        result.IsSuccess.ShouldBeFalse();
        // Nothing is taken on a refusal: a scheme added for an entry that was rejected would route
        // tokens to a handler no binding names.
        schemes.Added.ShouldBeEmpty();
    }

    [Fact]
    public void Refuses_an_entry_with_no_authority()
    {
        var schemes = new RecordingSchemeProvider();

        var result = new LocalKeyAuthenticationType().TakeScheme(
            Entry("FdwAuthority", null), schemes, Services(), null);

        result.IsSuccess.ShouldBeFalse();
        schemes.Added.ShouldBeEmpty();
    }

    [Fact]
    public void The_scheme_name_carries_the_prefix_the_options_bridge_reads_back()
    {
        // The bridge recovers the entry name by trimming this prefix, so the two have to agree.
        LocalKeyAuthenticationType.SchemeNameFor("FdwAuthority")
            .ShouldBe(LocalKeyAuthenticationType.SchemePrefix + "FdwAuthority");
    }

    private static IAuthenticationServiceConfiguration Entry(string? name, string? authority)
        => new AuthenticationServiceConfiguration
        {
            Name = name ?? string.Empty,
            ServiceOptionType = "LocalKey",
            Enabled = true,
            Authority = authority,
        };

    private static IServiceProvider Services() => new ServiceCollection().BuildServiceProvider();

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
