using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6394: the data store routes carry the working areas as names. A request that says nothing about them leaves the store's alone, one that sends an empty list
/// clears them, and one that sends names replaces them, keeping the Id of a row whose name the store already had; a response states the names the store keeps.
/// </summary>
public sealed class DataStoreWorkingPathsTests
{
    private static DataStoreImplementationConfiguration Store(params string[] names) => new()
    {
        Name = "Warehouse",
        WorkingPathRows = [.. names.Select(name => new DataStoreWorkingPathConfiguration { Id = Guid.CreateVersion7(), Name = name })],
    };

    [Fact]
    public void ARequestThatSaysNothingKeepsTheStoresWorkingAreas()
    {
        var store = Store("tmp");

        DataStoreWorkingPaths.Apply(store, null);

        store.WorkingPaths.ShouldBe(["tmp"]);
    }

    [Fact]
    public void AnEmptyListClearsThem()
    {
        var store = Store("tmp");

        DataStoreWorkingPaths.Apply(store, []);

        store.WorkingPathRows.ShouldBeEmpty();
    }

    [Fact]
    public void NamesReplaceThem()
    {
        var store = Store("tmp");

        DataStoreWorkingPaths.Apply(store, ["stage", "scratch"]);

        store.WorkingPaths.ShouldBe(["stage", "scratch"]);
    }

    [Fact]
    public void ARowWhoseNameTheStoreAlreadyHadKeepsItsIdWhateverTheCase()
    {
        var store = Store("tmp");
        var id = store.WorkingPathRows[0].Id;

        DataStoreWorkingPaths.Apply(store, ["TMP", "stage"]);

        store.WorkingPathRows.Single(row => row.Name == "TMP").Id.ShouldBe(id);
        store.WorkingPathRows.Single(row => row.Name == "stage").Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void AResponseStatesTheNamesTheStoreKeeps()
    {
        DataStoreWorkingPaths.Of(Store("tmp", "stage")).ShouldBe(["tmp", "stage"]);
    }

    [Fact]
    public void AStoreWithNoneStatesAnEmptyList()
    {
        DataStoreWorkingPaths.Of(Store()).ShouldBeEmpty();
    }

    [Fact]
    public void TheRequestsAndTheResponseCarryTheMember()
    {
        new CreateDataStoreRequest { WorkingPaths = ["tmp"] }.WorkingPaths.ShouldBe(["tmp"]);
        new UpdateDataStoreRequest { WorkingPaths = ["tmp"] }.WorkingPaths.ShouldBe(["tmp"]);
        new DataStoreDetailResponse { Name = "store" }.WorkingPaths.ShouldBeEmpty();
    }
}
