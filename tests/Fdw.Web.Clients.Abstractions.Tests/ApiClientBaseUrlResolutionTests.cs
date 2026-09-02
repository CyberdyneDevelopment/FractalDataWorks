using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using Fdw.Schema.Clients;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Fdw.Web.Clients.Abstractions.Tests;

/// <summary>
/// Where a named API client gets its base address.
/// </summary>
/// <remarks>
/// An API client is a service type option, so its endpoint is its own configuration and the platform
/// loads it through <see cref="IApiEndpointSource"/> when the client is resolved. A host declares
/// nothing in a file, which is why these assert against a registered source rather than against
/// configuration keys.
///
/// The endpoint is read INSIDE the factory's configure delegate, so it is resolved on each
/// CreateClient(name) rather than at registration — that is what lets a host register the ~35 client
/// types its package references bring in while declaring endpoints only for the ones it resolves.
/// </remarks>
public sealed class ApiClientBaseUrlResolutionTests
{
    private const string ClientName = "SchemaClient";

    private sealed class StubEndpointSource(string? clientName, string? endpoint) : IApiEndpointSource
    {
        public string? Resolve(string name)
            => string.Equals(name, clientName, StringComparison.Ordinal) ? endpoint : null;
    }

    private static Uri? ConfiguredBaseAddress(IApiEndpointSource? source)
    {
        var builder = Host.CreateApplicationBuilder();
        if (source is not null) builder.Services.AddSingleton(source);
        new SchemaClientType().Configure(builder);

        using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>().Get(ClientName);

        using var client = new HttpClient();
        foreach (var action in options.HttpClientActions) action(client);
        return client.BaseAddress;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureUsesTheEndpointTheSourceDeclaresForThisClient()
    {
        ConfiguredBaseAddress(new StubEndpointSource(ClientName, "http://declared/"))
            .ShouldBe(new Uri("http://declared/"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureIgnoresAnEndpointDeclaredForAnotherClient()
    {
        ConfiguredBaseAddress(new StubEndpointSource("SomeOtherClient", "http://other/"))
            .ShouldBeNull();
    }

    // Registration is unconditional and resolution is what makes a client required, so a client
    // nobody declared an endpoint for registers cleanly and is left with no BaseAddress -- the
    // absence is reported by name rather than filled in with an invented URL.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithNoEndpointDeclaredLeavesBaseAddressUnset()
    {
        ConfiguredBaseAddress(new StubEndpointSource(ClientName, null)).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithNoEndpointSourceAtAllLeavesBaseAddressUnset()
    {
        ConfiguredBaseAddress(source: null).ShouldBeNull();
    }
}
