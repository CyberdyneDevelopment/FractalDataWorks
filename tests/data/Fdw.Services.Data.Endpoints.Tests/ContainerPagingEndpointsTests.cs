using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Results;
using Fdw.Services.Multitenancy.Abstractions.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5562: the paging a container of a data store states for itself. The refusals of its own (a store that does not page its containers, a request that names no
/// style) answer 400 by their numbers, and the tenant gate is asked before anything is said about a store: a caller whose tenant may not change a store is told that,
/// and nothing of what the store is made of (its type, its paths, its connection).
/// </summary>
public sealed class ContainerPagingEndpointsTests
{
    private static readonly Guid StoreId = Guid.CreateVersion7();
    private static readonly Guid ContainerId = Guid.CreateVersion7();

    private sealed class SetEndpoint : SetDataStoreContainerPagingEndpointBase
    {
        public SetEndpoint(IDataStoreConfigurationProvider stores, IConnectionConfigurationProvider connections)
            : base(NullLogger<SetDataStoreContainerPagingEndpointBase>.Instance, stores, connections)
        {
        }

        protected override void ConfigureEndpoint()
        {
        }

        public Task<IGenericResult<DataStoreContainerPagingDto?>> Find(SetDataStoreContainerPagingRequest request, CancellationToken ct) => FindForUpdate(request, ct);
    }

    private sealed class AddEndpoint : AddDataStoreContainerEndpointBase
    {
        public AddEndpoint(IDataStoreConfigurationProvider stores, IConnectionConfigurationProvider connections)
            : base(NullLogger<AddDataStoreContainerEndpointBase>.Instance, stores, connections)
        {
        }

        protected override void ConfigureEndpoint()
        {
        }

        public Task<IGenericResult<DataStoreContainerResponse>> Add(AddDataStoreContainerRequest request, CancellationToken ct) => Create(request, ct);
    }

    private static DataStoreImplementationConfiguration OtherTenantsStore() => new()
    {
        Id = StoreId,
        Name = "Crm",
        Domain = "DataStore",
        Implementation = "MsSql",
        ConnectionId = Guid.CreateVersion7(),
        Paths = [new DataPathConfiguration { Name = "dbo", Containers = [new DataContainerConfiguration { Id = ContainerId, Name = "orders" }] }],
    };

    // The provider holds a store the caller's tenant may not change: the policy refuses the write, and anything else asked of the store would answer.
    private static Mock<IDataStoreConfigurationProvider> RefusingProvider()
    {
        IGenericResult refused = GenericResult.Failure(CrossTenantChangeLog.Refused(NullLogger.Instance, "CrossTenantDataStoreWrite", Guid.NewGuid(), Guid.NewGuid()));
        var provider = new Mock<IDataStoreConfigurationProvider>();
        provider
            .Setup(p => p.Get("Crm", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(OtherTenantsStore()));
        provider.Setup(p => p.PermitWrite(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(refused);
        return provider;
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task SettingAnotherTenantsContainerPagingAnswersForbiddenBeforeAnythingOfTheStoreIsRead()
    {
        var connections = new Mock<IConnectionConfigurationProvider>(MockBehavior.Strict);
        var endpoint = new SetEndpoint(RefusingProvider().Object, connections.Object);

        var found = await endpoint.Find(
            new SetDataStoreContainerPagingRequest { Name = "Crm", ContainerId = ContainerId, PagingStyle = "LinkHeader" }, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(found).ShouldBeTrue();
        connections.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AddingToAnotherTenantsStoreAnswersForbiddenBeforeItsPathsOrItsTypeAreAnswered()
    {
        var connections = new Mock<IConnectionConfigurationProvider>(MockBehavior.Strict);
        var endpoint = new AddEndpoint(RefusingProvider().Object, connections.Object);

        var added = await endpoint.Add(
            new AddDataStoreContainerRequest { Name = "Crm", PathName = "missing", ContainerName = "invoices", PagingStyle = "LinkHeader" }, TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(added).ShouldBeTrue();
        connections.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AStoreThatDoesNotPageItsContainersAnswersBadRequest()
    {
        var failure = GenericResult<int>.Failure(
            DataServiceResultCodes.ByName("ContainerPagingNotForStore"), NullLogger.Instance, ResultDetails.Create("container", "orders"));

        var (status, body) = ResultHttpStatusMapper.Map(failure, new DefaultHttpContext());

        status.ShouldBe(400);
        body.Extensions["code"].ShouldBe("DATA-21080");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ARequestThatNamesNoPagingStyleAnswersBadRequest()
    {
        var failure = GenericResult<int>.Failure(
            DataServiceResultCodes.ByName("ContainerPagingNothingStated"), NullLogger.Instance, ResultDetails.Create("container", "orders"));

        var (status, body) = ResultHttpStatusMapper.Map(failure, new DefaultHttpContext());

        status.ShouldBe(400);
        body.Extensions["code"].ShouldBe("DATA-21081");
    }
}
