using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Data;
namespace Fdw.Services.Connections.Tests;

/// <summary>
/// Tests for <see cref="ConnectionHealthService"/> — DataGateway-backed persistence and retrieval
/// of connection health check history against ops.ConnectionHealthCheck.
/// </summary>
public sealed class ConnectionHealthServiceTests
{
    // A stub rather than the real provider: this fixture is about what the service does with a
    // gateway, not about how one is supplied.
    private sealed class StubGatewayProvider(IConfigurationGateway gateway) : IConfigurationGatewayProvider
    {
        public List<string> Asked { get; } = [];

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
        {
            Asked.Add(connectionName);
            return GenericResult<IConfigurationGateway>.Success(gateway);
        }

        public IGenericResult Register(IConfigurationGateway other) => throw new NotSupportedException();
    }

    private sealed record Fixture(ConnectionHealthService Service, Mock<IConfigurationGateway> Gateway, StubGatewayProvider Provider);

    private static Fixture CreateService()
    {
        var gateway = new Mock<IConfigurationGateway>(MockBehavior.Loose);
        var provider = new StubGatewayProvider(gateway.Object);
        var service = new ConnectionHealthService(provider, NullLogger<ConnectionHealthService>.Instance);
        return new Fixture(service, gateway, provider);
    }

    // FDW-1068: the health history lives in the platform's own store, so it is written through the configuration
    // gateway onto that store and never through the request-facing Main gateway, which runs whatever store a request names.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task RecordHealthCheckWritesThroughThePlatformConfigurationGateway()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        await fixture.Service.RecordHealthCheck(
            Guid.NewGuid(), "TestConnection", true, 42, null, TestContext.Current.CancellationToken);

        fixture.Provider.Asked.ShouldBe(new[] { "PlatformConfiguration" });
    }

    private static void SetupExecuteInt(Mock<IConfigurationGateway> gateway, IGenericResult<int> result)
        => gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private static void SetupExecuteHistory(Mock<IConfigurationGateway> gateway, IGenericResult<IEnumerable<ConnectionHealthCheckRecord>> result)
        => gateway
            .Setup(g => g.Execute<IEnumerable<ConnectionHealthCheckRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    // ── RecordHealthCheck ───────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task RecordHealthCheckWithSuccessfulInsertReturnsSuccess()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var result = await fixture.Service.RecordHealthCheck(
            Guid.NewGuid(), "TestConnection", true, 42, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordHealthCheckWhenInsertFailsWithMessagePropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("insert boom")));

        var result = await fixture.Service.RecordHealthCheck(
            Guid.NewGuid(), "TestConnection", false, null, "probe failed", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("insert boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordHealthCheckWhenGatewayThrowsReturnsFailure()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.RecordHealthCheck(
            Guid.NewGuid(), "TestConnection", true, 10, null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    // ── GetHistory ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetHistoryReturnsResultsOrderedByCheckedAtDescending()
    {
        var fixture = CreateService();
        var connectionId = Guid.NewGuid();
        var older = new ConnectionHealthCheckRecord { ConnectionId = connectionId, CheckedAt = DateTimeOffset.UtcNow.AddMinutes(-10), IsHealthy = true };
        var newer = new ConnectionHealthCheckRecord { ConnectionId = connectionId, CheckedAt = DateTimeOffset.UtcNow, IsHealthy = false };
        SetupExecuteHistory(fixture.Gateway, GenericResult<IEnumerable<ConnectionHealthCheckRecord>>.Success([older, newer]));

        var result = await fixture.Service.GetHistory(connectionId, 20, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        result.Value![0].ShouldBe(newer);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetHistoryRespectsCountLimit()
    {
        var fixture = CreateService();
        var connectionId = Guid.NewGuid();
        var records = Enumerable.Range(0, 5)
            .Select(i => new ConnectionHealthCheckRecord { ConnectionId = connectionId, CheckedAt = DateTimeOffset.UtcNow.AddMinutes(-i) })
            .ToArray();
        SetupExecuteHistory(fixture.Gateway, GenericResult<IEnumerable<ConnectionHealthCheckRecord>>.Success(records));

        var result = await fixture.Service.GetHistory(connectionId, 2, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetHistoryWhenQueryFailsPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteHistory(fixture.Gateway, GenericResult<IEnumerable<ConnectionHealthCheckRecord>>.Failure(new GenericMessage("query boom")));

        var result = await fixture.Service.GetHistory(Guid.NewGuid(), 20, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("query boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetHistoryWhenGatewayThrowsReturnsFailure()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<ConnectionHealthCheckRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.GetHistory(Guid.NewGuid(), 20, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
}
