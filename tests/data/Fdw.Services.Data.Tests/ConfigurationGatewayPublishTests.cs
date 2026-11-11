using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A configuration gateway hands every change it is told of to the publisher, whether or not it keeps a cache itself,
/// since the other hosts may (FDW-4782).
/// </summary>
public sealed class ConfigurationGatewayPublishTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AChangeIsPublishedEvenWhenThisHostKeepsNoCache()
    {
        var publisher = new RecordingPublisher();
        var gateway = new ConfigurationGateway(
            "PlatformConfiguration",
            new NoConnections(),
            new ConfigurationSchema(),
            logger: null,
            cache: null,
            options: null,
            authenticationContextAccessor: null,
            publisher: publisher);

        gateway.InvalidateCachedResults(new DataStoreTarget("PlatformConfiguration", "conn", "Connection"));

        publisher.Published.ShouldBe(["conn.Connection"]);
    }

    private sealed class RecordingPublisher : ICacheChangePublisher
    {
        public List<string> Published { get; } = [];

        public void Publish(IReadOnlyList<string> tags) => Published.AddRange(tags);
    }

    private sealed class NoConnections : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
    {
        public Task<IGenericResult<IGenericConnection>> Create(IConnectionImplementationConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<IGenericConnection>>(GenericResult<IGenericConnection>.Failure(new GenericMessage("no connection in this test")));

        public Task<IGenericResult<Fdw.Abstractions.IGenericService>> Create(IImplementationConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<Fdw.Abstractions.IGenericService>>(GenericResult<Fdw.Abstractions.IGenericService>.Failure(new GenericMessage("no connection in this test")));
    }
}
