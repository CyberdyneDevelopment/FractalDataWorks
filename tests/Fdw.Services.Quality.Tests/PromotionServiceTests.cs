using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Data;
using Moq;

namespace Fdw.Services.Quality.Tests;

/// <summary>
/// Tests for <see cref="PromotionService"/> - environment promotion request lifecycle logic.
/// Only <see cref="IConfigurationGateway"/> is faked; <see cref="QualityConfigurationProvider"/>
/// and the underlying <c>ImplementationConfigurationProviderBase</c> run for real, matching how production
/// wires them. An empty <see cref="IConfigurationGateway.DataStores"/> tree means every header
/// lookup resolves as a root (no-parent) table and no child cascade is attempted - exactly what
/// PromotionService needs (it only reads Environment headers).
/// </summary>
public sealed class PromotionServiceTests
{
    private static (PromotionService Service, Mock<IConfigurationGateway> Gateway) CreateService()
    {
        var gatewayMock = new Mock<IConfigurationGateway>(MockBehavior.Loose);
        gatewayMock.SetupGet(g => g.DataStores).Returns(new List<IDataStore>());

        var lazyGateway = GatewayProviderFor(gatewayMock.Object);
        var qualityProvider = new QualityConfigurationProvider(
            NullLogger<QualityConfigurationProvider>.Instance,
            lazyGateway,
            "ConfigurationDb");

        var loggerFactory = LoggerFactory.Create(_ => { });
        return (new PromotionService(loggerFactory, qualityProvider), gatewayMock);
    }

    private static EnvironmentConfiguration MakeEnvironment(string name, int order = 0)
        => new() { Id = Guid.NewGuid(), Name = name, PromotionOrder = order, ConnectionName = "conn" };

    private static void SetupEnvironmentQuery(Mock<IConfigurationGateway> gateway, params EnvironmentConfiguration[] found)
    {
        gateway
            .Setup(g => g.Execute<IEnumerable<EnvironmentConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<EnvironmentConfiguration>>.Success(found));
    }

    private static void SetupEnvironmentQuerySequence(Mock<IConfigurationGateway> gateway, params IReadOnlyList<EnvironmentConfiguration>[] responses)
    {
        var sequence = gateway.SetupSequence(g => g.Execute<IEnumerable<EnvironmentConfiguration>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()));
        foreach (var response in responses)
        {
            sequence = sequence.ReturnsAsync(GenericResult<IEnumerable<EnvironmentConfiguration>>.Success(response));
        }
    }

    private static PromotionRequestConfiguration MakeRequest(string source = "Dev", string target = "Staging", string requestedBy = "alice")
        => new() { SourceEnvironment = source, TargetEnvironment = target, RequestedBy = requestedBy };

    // ── GetEnvironments ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetEnvironmentsReturnsOrderedByPromotionOrder()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuery(gateway, MakeEnvironment("Prod", 2), MakeEnvironment("Dev", 0), MakeEnvironment("Staging", 1));

        var result = await service.GetEnvironments(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(e => e.Name).ShouldBe(new[] { "Dev", "Staging", "Prod" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetEnvironmentsPropagatesGatewayFailure()
    {
        var (service, gateway) = CreateService();
        gateway
            .Setup(g => g.Execute<IEnumerable<EnvironmentConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<EnvironmentConfiguration>>.Failure(new GenericMessage("boom")));

        var result = await service.GetEnvironments(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    // ── CreateRequest ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateRequestWithSameSourceAndTargetReturnsFailureWithSameEnvironmentErrorCode()
    {
        var (service, _) = CreateService();

        var result = await service.CreateRequest(MakeRequest(source: "Dev", target: "dev"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-21000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateRequestWithUnknownSourceEnvironmentReturnsFailure()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuery(gateway); // empty -> not found

        var result = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31003");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateRequestWithUnknownTargetEnvironmentReturnsFailure()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(
            gateway,
            [MakeEnvironment("Dev")],
            []);

        var result = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31003");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task CreateRequestWithValidEnvironmentsReturnsSuccessAndPendingStatus()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(
            gateway,
            [MakeEnvironment("Dev")],
            [MakeEnvironment("Staging")]);

        var result = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldNotBe(Guid.Empty);
        result.Value!.Status.ShouldBe("Pending");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateRequestWhenGatewayThrowsReturnsFailureWithPromotionFailedCode()
    {
        var (service, gateway) = CreateService();
        gateway
            .Setup(g => g.Execute<IEnumerable<EnvironmentConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-91001");
    }

    // ── GetRequest / GetRequests ────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetRequestWithExistingIdReturnsSuccess()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        var created = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        var result = await service.GetRequest(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(created.Value!.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetRequestWithNonExistentIdReturnsFailure()
    {
        var (service, _) = CreateService();

        var result = await service.GetRequest(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31002");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetRequestsWithoutStatusFilterReturnsAllOrderedByCreatedAtDescending()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(
            gateway,
            [MakeEnvironment("Dev")], [MakeEnvironment("Staging")],
            [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        await service.CreateRequest(MakeRequest(requestedBy: "first"), TestContext.Current.CancellationToken);
        await Task.Delay(5, TestContext.Current.CancellationToken);
        var second = await service.CreateRequest(MakeRequest(requestedBy: "second"), TestContext.Current.CancellationToken);

        var result = await service.GetRequests(status: null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        result.Value![0].Id.ShouldBe(second.Value!.Id);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetRequestsWithStatusFilterIsCaseInsensitive()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        var result = await service.GetRequests(status: "PENDING", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetRequestsWithNonMatchingStatusFilterReturnsEmpty()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        var result = await service.GetRequests(status: "Rejected", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    // ── ApproveRequest / RejectRequest ──────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ApproveRequestWithExistingRequestSetsApprovedStatus()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        var created = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        var result = await service.ApproveRequest(created.Value!.Id, "bob", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.ShouldBe("Approved");
        result.Value!.ApprovedBy.ShouldBe("bob");
        result.Value!.ApprovedAt.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ApproveRequestWithNonExistentIdReturnsFailure()
    {
        var (service, _) = CreateService();

        var result = await service.ApproveRequest(Guid.NewGuid(), "bob", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31002");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task RejectRequestWithExistingRequestSetsRejectedStatusAndNotes()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        var created = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        var result = await service.RejectRequest(created.Value!.Id, "bob", "not ready", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.ShouldBe("Rejected");
        result.Value!.Notes.ShouldBe("not ready");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RejectRequestWithNonExistentIdReturnsFailure()
    {
        var (service, _) = CreateService();

        var result = await service.RejectRequest(Guid.NewGuid(), "bob", "reason", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31002");
    }

    // ── ExecutePromotion ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecutePromotionWithApprovedRequestReturnsSuccessAndCompletesRequest()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        var created = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);
        await service.ApproveRequest(created.Value!.Id, "bob", TestContext.Current.CancellationToken);

        var result = await service.ExecutePromotion(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestId.ShouldBe(created.Value!.Id);

        var updated = await service.GetRequest(created.Value!.Id, TestContext.Current.CancellationToken);
        updated.Value!.Status.ShouldBe("Completed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecutePromotionWithNonExistentRequestReturnsFailure()
    {
        var (service, _) = CreateService();

        var result = await service.ExecutePromotion(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31002");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecutePromotionWithUnapprovedRequestReturnsFailureWithNotApprovedCode()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);
        var created = await service.CreateRequest(MakeRequest(), TestContext.Current.CancellationToken);

        var result = await service.ExecutePromotion(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-41002");
    }

    // ── CompareEnvironments ─────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task CompareEnvironmentsWithBothFoundReturnsEmptyDiff()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], [MakeEnvironment("Staging")]);

        var result = await service.CompareEnvironments("Dev", "Staging", "DataSet", "Orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SourceEnvironment.ShouldBe("Dev");
        result.Value!.TargetEnvironment.ShouldBe("Staging");
        result.Value!.Differences.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CompareEnvironmentsWithUnknownSourceReturnsFailure()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuery(gateway); // empty -> not found for the first (source) lookup

        var result = await service.CompareEnvironments("Dev", "Staging", "DataSet", "Orders", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31003");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CompareEnvironmentsWithUnknownTargetReturnsFailure()
    {
        var (service, gateway) = CreateService();
        SetupEnvironmentQuerySequence(gateway, [MakeEnvironment("Dev")], []);

        var result = await service.CompareEnvironments("Dev", "Staging", "DataSet", "Orders", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31003");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CompareEnvironmentsWhenGatewayThrowsReturnsFailureWithPromotionFailedCode()
    {
        var (service, gateway) = CreateService();
        gateway
            .Setup(g => g.Execute<IEnumerable<EnvironmentConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await service.CompareEnvironments("Dev", "Staging", "DataSet", "Orders", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-91001");
    }

    // Why the gateway is registered rather than handed over: a provider asks for the gateway on the
    // connection it was told its rows live on, so the fake has to answer to that name to be found.
    // Why a double rather than the real provider: these tests exercise what a configuration provider
    // does with its gateway, not which gateway it selects, so the double answers for whatever
    // connection is asked. Selection itself is covered where the real provider is under test.
    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}
