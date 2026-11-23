using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>FDW-5446: the detail of a variation tells its owner what it varies and tells a person who holds only the variation nothing of it.</summary>
public sealed class VariationChainVisibilityTests
{
    private static ResolvedDataSet View()
    {
        var root = new SimpleDataSetConfiguration { Id = Guid.NewGuid(), Name = "Sales", Implementation = "Simple" };
        var view = new VariationDataSetConfiguration
        {
            Id = Guid.NewGuid(), Name = "west", VariationName = "west", Implementation = VariationDataSetConfiguration.ImplementationName,
            OwnerWorkspaceId = Guid.NewGuid(), BaseDataSetId = root.Id, ParentVariationId = Guid.NewGuid(), StoredFilters = "{}",
        };
        return new ResolvedDataSet(root, view, root.Name, ["{}"]);
    }

    private static DataSetDetailResponse DetailOf(ResolvedDataSet resolved) => DataSetQueryHelper.MapToDetail(resolved);

    private static IVariationChainAccess Answering(IGenericResult<bool> answer)
    {
        var access = new Mock<IVariationChainAccess>();
        access.Setup(a => a.MayShowChain(It.IsAny<IDataSetVariationImplementationConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(answer);
        return access.Object;
    }

    private static void ChainIsShown(DataSetDetailResponse detail)
    {
        detail.ParentName.ShouldBe("Sales");
        detail.BaseDataSetId.ShouldNotBeNull();
        detail.ParentVariationId.ShouldNotBeNull();
        detail.VariationFilters.ShouldBe("{}");
    }

    private static void ChainIsHidden(DataSetDetailResponse detail)
    {
        detail.ParentName.ShouldBeNull();
        detail.BaseDataSetId.ShouldBeNull();
        detail.ParentVariationId.ShouldBeNull();
        detail.VariationFilters.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task TheOwnerKeepsTheChainAndTheNameKindAndWorkspaceStay()
    {
        var resolved = View();
        var detail = DetailOf(resolved);

        var shown = await GetDataSetEndpointBase.ShowChainToOwner(Answering(GenericResult<bool>.Success(true)), resolved, detail, TestContext.Current.CancellationToken);

        shown.IsSuccess.ShouldBeTrue(shown.CurrentMessage);
        ChainIsShown(detail);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AHolderLosesTheChainButKeepsTheViewsNameKindAndOwningWorkspace()
    {
        var resolved = View();
        var detail = DetailOf(resolved);

        var shown = await GetDataSetEndpointBase.ShowChainToOwner(Answering(GenericResult<bool>.Success(false)), resolved, detail, TestContext.Current.CancellationToken);

        shown.IsSuccess.ShouldBeTrue(shown.CurrentMessage);
        ChainIsHidden(detail);
        detail.Name.ShouldBe("west");
        detail.VariationKind.ShouldBe(VariationKinds.Scoped);
        detail.OwnerWorkspaceId.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task ARouteBuiltWithNoAnswerShowsNobodyTheChain()
    {
        var resolved = View();
        var detail = DetailOf(resolved);

        var shown = await GetDataSetEndpointBase.ShowChainToOwner(null, resolved, detail, TestContext.Current.CancellationToken);

        shown.IsSuccess.ShouldBeTrue(shown.CurrentMessage);
        ChainIsHidden(detail);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task OwnershipThatCannotBeDecidedIsAFailureAndNeverShownAsAnAnswer()
    {
        var resolved = View();
        var detail = DetailOf(resolved);
        var undecided = GenericResult<bool>.Failure(new Fdw.Messages.GenericMessage("workspace unavailable"));

        var shown = await GetDataSetEndpointBase.ShowChainToOwner(Answering(undecided), resolved, detail, TestContext.Current.CancellationToken);

        shown.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task ADataSetThatIsNotAVariationIsLeftAsItWasAndNeverAsked()
    {
        var resolved = new ResolvedDataSet(new SimpleDataSetConfiguration { Id = Guid.NewGuid(), Name = "Sales", Implementation = "Simple" }, null, null, []);
        var detail = DetailOf(resolved);
        var access = new Mock<IVariationChainAccess>(MockBehavior.Strict);

        var shown = await GetDataSetEndpointBase.ShowChainToOwner(access.Object, resolved, detail, TestContext.Current.CancellationToken);

        shown.IsSuccess.ShouldBeTrue(shown.CurrentMessage);
        access.VerifyNoOtherCalls();
    }
}
