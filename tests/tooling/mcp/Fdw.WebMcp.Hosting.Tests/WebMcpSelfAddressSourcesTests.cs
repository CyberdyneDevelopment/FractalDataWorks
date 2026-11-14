using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// Where a host's own address comes from (FDW-5721): the sources there are, and each reason the listening
/// address gives none, with its own message.
/// </summary>
public class WebMcpSelfAddressSourcesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheSourcesAreTheListeningAddressAndAFixedAddressAndNothingReadsTheHostHeader()
    {
        string.Join(",", WebMcpSelfAddressSources.All().Select(static source => source.Name).OrderBy(static name => name, StringComparer.Ordinal))
            .ShouldBe("FixedAddress,ListeningAddress");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheListeningAddressIsTheOneAddressTheServerBound()
    {
        var result = Listening(Server("http://127.0.0.1:5511"));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(new Uri("http://127.0.0.1:5511"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AWildcardBindBesideACallableAddressIsPassedOver()
    {
        var result = Listening(Server("http://0.0.0.0:8080", "http://127.0.0.1:5511"));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(new Uri("http://127.0.0.1:5511"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AServerWithNoAddressListIsRefused()
    {
        var result = Listening(new FakeServer(new FeatureCollection()));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("exposes no address list");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AServerThatHasBoundNothingIsRefused()
    {
        // The list is empty before Kestrel starts; this is why the address is never read at Initialize.
        var result = Listening(Server());

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("found no address this host can call itself on");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("http://0.0.0.0:5511")]
    [InlineData("http://[::]:5511")]
    [InlineData("http://+:5511")]
    [InlineData("http://*:5511")]
    public void OnlyAWildcardBindIsRefused(string address)
    {
        var result = Listening(Server(address));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("found no address this host can call itself on");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MoreThanOneCallableAddressIsRefusedRatherThanOnePicked()
    {
        var result = Listening(Server("http://127.0.0.1:5511", "https://127.0.0.1:5512"));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("found more than one address");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheFixedAddressIsTheRowsWhateverTheServerListensOn()
    {
        var source = WebMcpSelfAddressSources.ByName("FixedAddress");
        var settings = new WebMcpHostSettings("X-Webmcp-Key", ClientApiKey: null, source, new Uri("http://10.0.0.5:8080"));

        var result = source.Resolve(settings, Server("http://127.0.0.1:5511"), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(new Uri("http://10.0.0.5:8080"));
    }

    private static IGenericResult<Uri> Listening(IServer server)
    {
        var source = WebMcpSelfAddressSources.ByName("ListeningAddress");
        return source.Resolve(new WebMcpHostSettings("X-Webmcp-Key", ClientApiKey: null, source, SelfBaseAddress: null), server, NullLogger.Instance);
    }

    private static FakeServer Server(params string[] addresses)
    {
        var feature = new ServerAddressesFeature();
        foreach (var address in addresses)
        {
            feature.Addresses.Add(address);
        }

        var features = new FeatureCollection();
        features.Set<IServerAddressesFeature>(feature);
        return new FakeServer(features);
    }

    /// <summary>A server that reports the features a test gives it and serves nothing.</summary>
    private sealed class FakeServer(IFeatureCollection features) : IServer
    {
        public IFeatureCollection Features { get; } = features;

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
