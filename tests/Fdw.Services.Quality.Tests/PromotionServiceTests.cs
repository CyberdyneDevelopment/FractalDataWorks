using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Data;
using Moq;

namespace Fdw.Services.Quality.Tests;

/// <summary>
/// Tests for <see cref="PromotionService"/> - environment promotion request lifecycle logic.
/// Only <see cref="IConfigurationGateway"/> is faked; <see cref="EnvironmentConfigurationProvider"/>
/// runs for real, matching how production wires it. An empty <see cref="IConfigurationGateway.DataStores"/> tree means every header
/// lookup resolves as a root (no-parent) table and no child cascade is attempted - exactly what
/// PromotionService needs (it only reads Environment headers).
/// </summary>
public sealed class PromotionServiceTests
{
    private static (PromotionService Service, EnvironmentStore Gateway) CreateService()
    {
        var store = new EnvironmentStore();
        var environments = new EnvironmentConfigurationProvider(
            NullLogger<EnvironmentConfigurationProvider>.Instance,
            GatewayProviderFor(store.Gateway.Object), "PlatformConfiguration", "quality", "Environment");
        environments.Register("Environment", store.Implementations.Object);

        var loggerFactory = LoggerFactory.Create(_ => { });
        return (new PromotionService(loggerFactory, environments), store);
    }

    private static EnvironmentImplementationConfiguration MakeEnvironment(string name, int order = 0)
        => new() { Id = Guid.NewGuid(), Name = name, PromotionOrder = order, ConnectionName = "conn" };

    private static void SetupEnvironmentQuery(EnvironmentStore gateway, params EnvironmentImplementationConfiguration[] found)
        => gateway.Holds(found);

    private static void SetupEnvironmentQuerySequence(EnvironmentStore gateway, params IReadOnlyList<EnvironmentImplementationConfiguration>[] responses)
        => gateway.HoldsInTurn(responses);

    private static PromotionRequestImplementationConfiguration MakeRequest(string source = "Dev", string target = "Staging", string requestedBy = "alice")
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
        gateway.CannotBeRead(new GenericMessage("boom"));

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
        gateway.ReadThrows(new InvalidOperationException("boom"));

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
        gateway.ReadThrows(new InvalidOperationException("boom"));

        var result = await service.CompareEnvironments("Dev", "Staging", "DataSet", "Orders", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-91001");
    }

    // The environment provider is a domain provider: it reads Environment domain rows and hands each to
    // the implementation provider registered for its Implementation. This serves both halves the way the
    // configuration store does, and honours the Name/Id filter a by-name read carries. The domain provider
    // takes the FIRST row a by-name read returns, so a store that ignored the name would answer with the
    // wrong environment.
    private sealed class EnvironmentStore
    {
        private readonly List<EnvironmentImplementationConfiguration> _held = [];
        private readonly Dictionary<Guid, EnvironmentImplementationConfiguration> _byId = [];
        private readonly Queue<IReadOnlyList<EnvironmentImplementationConfiguration>> _inTurn = new();
        private IGenericMessage? _failure;
        private Exception? _error;

        public EnvironmentStore()
        {
            Gateway.SetupGet(g => g.DataStores).Returns(new List<IDataStore>());
            Gateway
                .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataStoreTarget _, CancellationToken _) => Read(command));
            // A list read asks for each environment whole; a single read asks for it to a depth.
            Implementations
                .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => _byId.TryGetValue(id, out var environment)
                    ? GenericResult<IEnvironmentImplementationConfiguration>.Success(environment)
                    : GenericResult<IEnvironmentImplementationConfiguration>.Success(default!));
            Implementations
                .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, int _, CancellationToken _) => _byId.TryGetValue(id, out var environment)
                    ? GenericResult<IEnvironmentImplementationConfiguration>.Success(environment)
                    : GenericResult<IEnvironmentImplementationConfiguration>.Success(default!));
        }

        public Mock<IConfigurationGateway> Gateway { get; } = new(MockBehavior.Loose);

        public Mock<IEnvironmentImplementationConfigurationProvider> Implementations { get; } = new();

        /// <summary>Every read is answered from these environments, filtered by the name or id it asks for.</summary>
        public void Holds(params EnvironmentImplementationConfiguration[] environments)
        {
            foreach (var environment in environments)
            {
                _held.Add(environment);
                _byId[environment.Id] = environment;
            }
        }

        /// <summary>Each read, in order, is answered with the next of these, whatever it asks for.</summary>
        public void HoldsInTurn(params IReadOnlyList<EnvironmentImplementationConfiguration>[] reads)
        {
            foreach (var read in reads)
            {
                _inTurn.Enqueue(read);
                foreach (var environment in read) _byId[environment.Id] = environment;
            }
        }

        public void CannotBeRead(IGenericMessage message) => _failure = message;

        public void ReadThrows(Exception error) => _error = error;

        private Task<IGenericResult<IEnumerable<DomainConfiguration>>> Read(IDataCommand command)
        {
            if (_error is not null) return Task.FromException<IGenericResult<IEnumerable<DomainConfiguration>>>(_error);
            if (_failure is not null) return Task.FromResult(GenericResult<IEnumerable<DomainConfiguration>>.Failure(_failure));

            var answered = _inTurn.Count > 0 ? _inTurn.Dequeue() : Matching(command);
            return Task.FromResult(GenericResult<IEnumerable<DomainConfiguration>>.Success(
                answered.Select(environment => new DomainConfiguration
                {
                    Id = environment.Id,
                    Name = environment.Name,
                    Domain = "Environment",
                    Implementation = "Environment",
                }).ToList()));
        }

        private List<EnvironmentImplementationConfiguration> Matching(IDataCommand command)
        {
            if (command is not IQueryCommand { Filter.Root: { } root }) return _held;

            var identity = Conditions(root)
                .Where(condition => condition.PropertyName is "Name" or "Id")
                .ToList();

            return [.. _held.Where(environment => identity.TrueForAll(condition => condition.PropertyName switch
            {
                "Name" => string.Equals(environment.Name, condition.Value as string, StringComparison.Ordinal),
                "Id" => condition.Value is Guid id && environment.Id == id,
                _ => true,
            }))];
        }

        private static IEnumerable<IFilterCondition> Conditions(IFilterNode node)
        {
            switch (node)
            {
                case IFilterCondition condition:
                    yield return condition;
                    break;
                case FilterGroup group:
                    foreach (var child in group.Nodes)
                        foreach (var condition in Conditions(child))
                            yield return condition;
                    break;
                default:
                    break;
            }
        }
    }

    private static AnyConnectionGateways GatewayProviderFor(IConfigurationGateway gateway)
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
