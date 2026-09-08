using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema.Clients;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Fdw.Web.Clients.Abstractions.Tests;

/// <summary>
/// Where a named API client gets its base address.
/// </summary>
/// <remarks>
/// An endpoint IS a connection: a client named SchemaClient is served by the conn.Connection row of
/// that name, and its implementation's BaseUrl is the answer. So these assert against a registered
/// connection provider rather than against configuration keys.
///
/// The endpoint is read INSIDE the factory's configure delegate, so it is resolved on each
/// CreateClient(name) rather than at registration — that is what lets a host register the ~35 client
/// types its package references bring in while declaring endpoints only for the ones it resolves.
/// </remarks>
public sealed class ApiClientBaseUrlResolutionTests
{
    private const string ClientName = "SchemaClient";

    /// <summary>Answers for one connection name and nothing else.</summary>
    private sealed class StubConnections(string? connectionName, string? baseUrl) : IConnectionConfigurationProvider
    {
        public Task<IGenericResult<IConnectionImplementationConfiguration>> Get(
            string name, CancellationToken cancellationToken = default)
            => Task.FromResult(
                string.Equals(name, connectionName, StringComparison.Ordinal) && baseUrl is not null
                    ? GenericResult<IConnectionImplementationConfiguration>.Success(
                        new HttpConnectionConfiguration { BaseUrl = baseUrl })
                    : GenericResult<IConnectionImplementationConfiguration>.Success(default!));

        public Task<IGenericResult<IConnectionImplementationConfiguration>> Get(
            Guid id, CancellationToken cancellationToken = default)
            => Get(string.Empty, cancellationToken);

        public Task<IGenericResult> Save<T>(
            string serviceOptionType, string name, T implementationConfiguration,
            CancellationToken cancellationToken = default)
            where T : IConnectionImplementationConfiguration
            => Task.FromResult(GenericResult.Success());

        public Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult.Success());

        public Task<IGenericResult> Delete(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult.Success());

        public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
            where T : IImplementationConfigurationProvider<IConnectionImplementationConfiguration>
            => GenericResult.Success();
    }

    private static Uri? ConfiguredBaseAddress(IConnectionConfigurationProvider? connections)
    {
        var builder = Host.CreateApplicationBuilder();
        if (connections is not null) builder.Services.AddSingleton(connections);
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
    public void ConfigureUsesTheEndpointTheConnectionOfThatNameDeclares()
    {
        ConfiguredBaseAddress(new StubConnections(ClientName, "http://declared/"))
            .ShouldBe(new Uri("http://declared/"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureIgnoresAConnectionDeclaredUnderAnotherName()
    {
        ConfiguredBaseAddress(new StubConnections("SomeOtherClient", "http://other/"))
            .ShouldBeNull();
    }

    // Registration is unconditional and resolution is what makes a client required, so a client
    // nobody declared a connection for registers cleanly and is left with no BaseAddress -- the
    // absence is reported by name rather than filled in with an invented URL.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithNoConnectionForThisClientLeavesBaseAddressUnset()
    {
        ConfiguredBaseAddress(new StubConnections(ClientName, null)).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureWithNoConnectionProviderAtAllLeavesBaseAddressUnset()
    {
        ConfiguredBaseAddress(connections: null).ShouldBeNull();
    }
}
