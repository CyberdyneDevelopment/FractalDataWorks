using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Fdw.Schema.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Fdw.Web.Clients.Abstractions.Tests;

/// <summary>
/// Tests base-URL resolution for API client types. Every client type registers its OWN named
/// HttpClient keyed by its client name, so per-client endpoints were always physically possible —
/// but each call site read the flat <c>ApiClients:BaseUrl</c>, collapsing them onto one URL and
/// leaving the per-client shape unread. Reference.Api declares
/// <c>ApiClients:PipelineJobClient:BaseUrl</c> / <c>ApiClients:ScheduleClient:BaseUrl</c> to reach the
/// ETL and Scheduler hosts and has no flat key, so those clients got no BaseAddress at all.
/// </summary>
/// <remarks>
/// SchemaClientType is used as a representative concrete option: resolution lives on the shared
/// ApiClientTypeBase, so exercising one real client type covers the path every client type takes.
/// </remarks>
public sealed class ApiClientBaseUrlResolutionTests
{
    private const string ClientName = "SchemaClient";

    private static IConfiguration Config(params (string Key, string Value)[] entries)
    {
        var data = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in entries) data[key] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    // Why the options are inspected rather than IHttpClientFactory.CreateClient: the registration also
    // attaches BearerTokenHandler, whose own DI graph is irrelevant here. Replaying the configured
    // HttpClientActions asserts exactly what Configure decided, and nothing else.
    private static Uri? ConfiguredBaseAddress(IConfiguration configuration)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        new SchemaClientType().Configure(builder);

        using var provider = builder.Services.BuildServiceProvider();

        // Why GetRequiredService: registration is now unconditional, so the factory's options services are
        // always present. An undeclared endpoint shows up as an unset BaseAddress plus a logged error, not
        // as a missing registration.
        var options = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>().Get(ClientName);

        using var client = new HttpClient();
        foreach (var action in options.HttpClientActions) action(client);
        return client.BaseAddress;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithOnlyHostWideBaseUrlUsesIt()
    {
        ConfiguredBaseAddress(Config(("ApiClients:BaseUrl", "http://host-wide/")))
            .ShouldBe(new Uri("http://host-wide/"));
    }

    // The reference-api case: a per-client entry and NO flat key. Before the fix this registered nothing.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithOnlyPerClientBaseUrlUsesIt()
    {
        ConfiguredBaseAddress(Config(($"ApiClients:{ClientName}:BaseUrl", "http://per-client/")))
            .ShouldBe(new Uri("http://per-client/"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithBothPrefersThePerClientBaseUrl()
    {
        ConfiguredBaseAddress(Config(
            ("ApiClients:BaseUrl", "http://host-wide/"),
            ($"ApiClients:{ClientName}:BaseUrl", "http://per-client/")))
            .ShouldBe(new Uri("http://per-client/"));
    }

    // Why: a per-client entry for a DIFFERENT client must not leak into this one — that is the whole
    // point of splitting a host's clients across endpoints.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureIgnoresAnotherClientsPerClientBaseUrl()
    {
        ConfiguredBaseAddress(Config(
            ("ApiClients:BaseUrl", "http://host-wide/"),
            ("ApiClients:SomeOtherClient:BaseUrl", "http://other/")))
            .ShouldBe(new Uri("http://host-wide/"));
    }

    // Why the client is still registered, with no BaseAddress: NO FALLBACKS — no endpoint is invented when
    // the host declares none. Registration itself is unconditional so that "required" can mean "something
    // resolved it": a host registers every client type in every package it references, and demanding an
    // endpoint at registration would force it to invent URLs for clients it never calls. The absence is
    // reported by ApiEndpointLog.EndpointNotDeclared at resolution, naming the client and every key that
    // would satisfy it, rather than by throwing — this delegate runs while a scope realizes a client, often
    // inside a Blazor render, where an exception tears down the circuit and buries the cause.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithNoBaseUrlDeclaredLeavesBaseAddressUnset()
    {
        ConfiguredBaseAddress(Config()).ShouldBeNull();
    }
}
