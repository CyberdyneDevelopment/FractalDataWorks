using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-2390: a name resolves to a data set, or to a variation of one such as a saved view, and one name never means two things.
/// </summary>
public sealed class DataSetResolverTests
{
    private static readonly SimpleDataSetConfiguration Players = new() { Id = Guid.NewGuid(), Name = "AfcPlayers", Implementation = "Simple" };

    [Fact]
    public async Task A_data_set_resolves_to_itself_with_no_parent()
    {
        var resolver = Build(dataSets: [Players], variations: []);

        var result = await resolver.Resolve("AfcPlayers", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.DataSet.ShouldBeSameAs(Players);
        result.Value.Kind.ShouldBe(ResolvedDataSet.DataSetKind);
        result.Value.Name.ShouldBe("AfcPlayers");
        result.Value.ParentName.ShouldBeNull();
    }

    [Fact]
    public async Task A_view_of_a_data_set_resolves_to_the_data_set_it_varies_and_names_it_as_its_parent()
    {
        var view = View("weekly-totals", Players.Id, parent: null);
        var resolver = Build(dataSets: [Players], variations: [view]);

        var result = await resolver.Resolve("weekly-totals", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.DataSet.ShouldBeSameAs(Players);
        result.Value.Variation.ShouldBeSameAs(view);
        result.Value.Kind.ShouldBe("DataView");
        result.Value.Name.ShouldBe("weekly-totals");
        result.Value.Id.ShouldBe(view.Id);
        result.Value.ParentName.ShouldBe("AfcPlayers");
    }

    [Fact]
    public async Task A_view_of_a_view_names_the_view_it_narrows_as_its_parent()
    {
        var parent = View("weekly-totals", Players.Id, parent: null);
        var child = View("weekly-totals-east", Players.Id, parent: parent.Id);
        var resolver = Build(dataSets: [Players], variations: [parent, child]);

        var result = await resolver.Resolve("weekly-totals-east", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.DataSet.ShouldBeSameAs(Players);
        result.Value.ParentName.ShouldBe("weekly-totals");
    }

    [Fact]
    public async Task A_data_set_has_no_view_filters()
    {
        var resolver = Build(dataSets: [Players], variations: []);

        var result = await resolver.Resolve("AfcPlayers", TestContext.Current.CancellationToken);

        result.Value!.ViewFilters.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_filters_of_a_chain_are_collected_topmost_parent_first_and_the_view_itself_last()
    {
        var top = View("top", Players.Id, parent: null, filters: "TOP");
        var middle = View("middle", Players.Id, parent: top.Id, filters: "MIDDLE");
        var leaf = View("leaf", Players.Id, parent: middle.Id, filters: "LEAF");
        var resolver = Build(dataSets: [Players], variations: [top, middle, leaf]);

        var result = await resolver.Resolve("leaf", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ViewFilters.ShouldBe(new[] { "TOP", "MIDDLE", "LEAF" });
        result.Value.ParentName.ShouldBe("middle");
    }

    [Fact]
    public async Task A_view_with_no_filter_in_its_chain_adds_none_and_blank_filters_are_left_out()
    {
        var top = View("top", Players.Id, parent: null, filters: "  ");
        var leaf = View("leaf", Players.Id, parent: top.Id, filters: "LEAF");
        var resolver = Build(dataSets: [Players], variations: [top, leaf]);

        var result = await resolver.Resolve("leaf", TestContext.Current.CancellationToken);

        result.Value!.ViewFilters.ShouldBe(new[] { "LEAF" });
    }

    [Fact]
    public async Task A_chain_deeper_than_the_limit_is_refused_and_not_cut_short()
    {
        var chain = new List<DataViewDataSetVariationConfiguration> { View("v0", Players.Id, parent: null, filters: "F0") };
        for (var i = 1; i <= DataSetResolver.MaxChainDepth; i++)
            chain.Add(View($"v{i}", Players.Id, parent: chain[i - 1].Id, filters: $"F{i}"));
        var resolver = Build(dataSets: [Players], variations: [.. chain]);

        var result = await resolver.Resolve($"v{DataSetResolver.MaxChainDepth}", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task A_view_whose_parent_is_gone_is_refused_and_its_filters_are_not_dropped()
    {
        var orphan = View("leaf", Players.Id, parent: Guid.NewGuid(), filters: "LEAF");
        var resolver = Build(dataSets: [Players], variations: [orphan]);

        var result = await resolver.Resolve("leaf", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task A_name_nothing_has_resolves_to_nothing()
    {
        var resolver = Build(dataSets: [Players], variations: []);

        var result = await resolver.Resolve("nothing-here", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task A_name_held_by_a_data_set_and_a_view_is_refused_and_not_resolved_by_picking_one()
    {
        var clash = View("AfcPlayers", Players.Id, parent: null);
        var resolver = Build(dataSets: [Players], variations: [clash]);

        var result = await resolver.Resolve("AfcPlayers", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task A_view_whose_data_set_is_gone_fails_loudly_and_is_not_a_not_found()
    {
        var orphan = View("weekly-totals", Guid.NewGuid(), parent: null);
        var resolver = Build(dataSets: [], variations: [orphan]);

        var result = await resolver.Resolve("weekly-totals", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task A_read_that_fails_is_returned_as_it_is()
    {
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Failure(new GenericMessage("data sets unavailable")));
        var resolver = new DataSetResolver(dataSets.Object, new Mock<IDataSetVariationConfigurationProvider>().Object, NullLogger<DataSetResolver>.Instance);

        var result = await resolver.Resolve("AfcPlayers", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("data sets unavailable");
    }

    private static DataViewDataSetVariationConfiguration View(string name, Guid dataSetId, Guid? parent, string? filters = null)
        => new() { Id = Guid.NewGuid(), Name = name, VariationName = name, Implementation = DataViewDataSetVariationConfiguration.ImplementationName, BaseDataSetId = dataSetId, ParentVariationId = parent, Filters = filters };

    private static DataSetResolver Build(IDataSetImplementationConfiguration[] dataSets, IDataSetVariationImplementationConfiguration[] variations)
    {
        var sets = new Mock<IDataSetConfigurationProvider>();
        sets.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((string name, int _, CancellationToken _) =>
                Task.FromResult<IGenericResult<IDataSetImplementationConfiguration>>(
                    GenericResult<IDataSetImplementationConfiguration>.Success(Array.Find(dataSets, d => d.Name == name)!)));
        sets.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, int _, CancellationToken _) =>
                Task.FromResult<IGenericResult<IDataSetImplementationConfiguration>>(
                    GenericResult<IDataSetImplementationConfiguration>.Success(Array.Find(dataSets, d => d.Id == id)!)));

        var views = new Mock<IDataSetVariationConfigurationProvider>();
        views.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((string name, int _, CancellationToken _) =>
                Task.FromResult<IGenericResult<IDataSetVariationImplementationConfiguration>>(
                    GenericResult<IDataSetVariationImplementationConfiguration>.Success(Array.Find(variations, v => v.Name == name)!)));
        views.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, int _, CancellationToken _) =>
                Task.FromResult<IGenericResult<IDataSetVariationImplementationConfiguration>>(
                    GenericResult<IDataSetVariationImplementationConfiguration>.Success(Array.Find(variations, v => v.Id == id)!)));

        return new DataSetResolver(sets.Object, views.Object, NullLogger<DataSetResolver>.Instance);
    }
}
