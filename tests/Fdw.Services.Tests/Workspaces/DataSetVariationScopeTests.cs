using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-1032: the filter a chain of views puts on a view is every stored filter, parent first, combined with AND; a chain
/// that is too deep, has lost a parent, or holds a stored filter that is not a filter tree is refused, not run.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class DataSetVariationScopeTests
{
    private const string Grade = """{"field":"grade","operator":"Equal","value":"07"}""";
    private const string Campus = """{"field":"campus","operator":"Equal","value":"North"}""";
    private const string Week = """{"field":"week","operator":"GreaterThan","value":"3"}""";

    private readonly Mock<IDataSetVariationConfigurationProvider> _variations = new();

    [Fact]
    public async Task AViewWithNoFilterAnywhereInItsChainHasNoScope()
    {
        var result = await ScopeOf(Leaf(null, null));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task AViewsOwnFilterIsItsScope()
    {
        var result = await ScopeOf(Leaf(null, Grade));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Field.ShouldBe("grade");
        result.Value.Nodes.ShouldBeNull();
    }

    [Fact]
    public async Task AChainsFiltersAreCombinedWithAndParentFirst()
    {
        var root = Guid.NewGuid();
        var mid = Guid.NewGuid();
        Stored(root, null, Grade);
        Stored(mid, root, Campus);

        var result = await ScopeOf(Leaf(mid, Week));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Logic.ShouldBe("And");
        result.Value.Nodes!.Select(n => n!.Field).ToArray().ShouldBe(new[] { "grade", "campus", "week" });
    }

    [Fact]
    public async Task AViewInTheMiddleOfTheChainWithNoFilterAddsNothing()
    {
        var root = Guid.NewGuid();
        var mid = Guid.NewGuid();
        Stored(root, null, Grade);
        Stored(mid, root, null);

        var result = await ScopeOf(Leaf(mid, "  "));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Field.ShouldBe("grade");
    }

    [Fact]
    public async Task AStoredFilterThatIsNotAFilterTreeIsRefused()
    {
        var result = await ScopeOf(Leaf(null, "not json"));

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AParentThatNoLongerExistsRefusesTheView()
    {
        var gone = Guid.NewGuid();
        _variations
            .Setup(v => v.Get(gone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetVariationImplementationConfiguration>.Success(null!));

        var result = await ScopeOf(Leaf(gone, null));

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AChainDeeperThanTheCapIsRefusedNotFollowed()
    {
        var parent = (Guid?)null;
        for (var i = 0; i < DataSetVariationChain.MaxDepth; i++)
        {
            var id = Guid.NewGuid();
            Stored(id, parent, null);
            parent = id;
        }

        var result = await ScopeOf(Leaf(parent, null));

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AChainOfExactlyTheCapIsAllowed()
    {
        var parent = (Guid?)null;
        for (var i = 0; i < DataSetVariationChain.MaxDepth - 1; i++)
        {
            var id = Guid.NewGuid();
            Stored(id, parent, null);
            parent = id;
        }

        var result = await ScopeOf(Leaf(parent, null));

        result.IsSuccess.ShouldBeTrue();
    }

    // The chain is walked by DataSetVariationChain and each stored filter read by DataSetVariationScope; a view's scope is both.
    private async Task<IGenericResult<QueryFilterNode?>> ScopeOf(DataSetVariationDomainConfiguration view)
    {
        var stored = await DataSetVariationChain.StoredFilters(_variations.Object, view, NullLogger.Instance, TestContext.Current.CancellationToken);
        return stored.IsFailure
            ? stored.ToNewResult<QueryFilterNode?>()
            : DataSetVariationScope.Of(stored.Value!, view.VariationName, NullLogger.Instance);
    }

    private void Stored(Guid id, Guid? parent, string? filters)
        => _variations
            .Setup(v => v.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetVariationImplementationConfiguration>.Success(
                new ScopedDataSetVariationConfiguration { Id = id, VariationName = $"view-{id:N}", ParentVariationId = parent, Filters = filters }));

    private static DataSetVariationDomainConfiguration Leaf(Guid? parent, string? filters)
        => new() { VariationName = "leaf", ParentVariationId = parent, Filters = filters };
}
