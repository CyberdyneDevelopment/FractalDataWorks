using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.TestHelper;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Connections.Validation;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests.Validation;

/// <summary>
/// FDW-6394: a data store keeps its working areas as rows of its own, each a name. A name is a path or schema name, so it is not blank and fits the column; the rows of one
/// store do not repeat a name, whatever its case. The store reads them back as the names lineage judges a table by, and a store with no rows has no working area.
/// </summary>
public sealed class DataStoreWorkingPathValidatorTests
{
    private static DataStoreImplementationConfiguration Store(params string[] workingPaths) => new()
    {
        Name = "Warehouse",
        ConnectionId = Guid.NewGuid(),
        WorkingPathRows = [.. workingPaths.Select(name => new DataStoreWorkingPathConfiguration { Name = name })],
    };

    [Theory]
    [InlineData("tmp")]
    [InlineData("stage_1")]
    [InlineData("TMP")]
    public void ANameThatFitsTheColumnIsValid(string name)
    {
        new DataStoreWorkingPathConfigurationValidator()
            .TestValidate(new DataStoreWorkingPathConfiguration { Name = name })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankNameIsRefused(string name)
    {
        new DataStoreWorkingPathConfigurationValidator()
            .TestValidate(new DataStoreWorkingPathConfiguration { Name = name })
            .ShouldHaveValidationErrorFor(row => row.Name);
    }

    [Fact]
    public void ANameLongerThanTheColumnIsRefused()
    {
        new DataStoreWorkingPathConfigurationValidator()
            .TestValidate(new DataStoreWorkingPathConfiguration { Name = new string('a', 501) })
            .ShouldHaveValidationErrorFor(row => row.Name);
    }

    [Fact]
    public void AStoreWhoseRowsStateOneNameTwiceIsRefusedWhateverTheCase()
    {
        new DataStoreConfigurationValidator()
            .TestValidate(Store("tmp", "TMP"))
            .ShouldHaveValidationErrorFor(store => store.WorkingPathRows);
    }

    [Fact]
    public void AStoreWhoseRowsStateDifferentNamesIsValid()
    {
        new DataStoreConfigurationValidator()
            .TestValidate(Store("tmp", "stage"))
            .ShouldNotHaveValidationErrorFor(store => store.WorkingPathRows);
    }

    [Fact]
    public void AStoreWithABlankRowIsRefused()
    {
        new DataStoreConfigurationValidator()
            .TestValidate(Store("tmp", " "))
            .ShouldHaveValidationErrorFor("WorkingPathRows[1].Name");
    }

    [Fact]
    public void TheStoreReadsItsRowsBackAsTheNamesLineageJudgesATableBy()
    {
        Store("tmp", "stage").WorkingPaths.ShouldBe(["tmp", "stage"]);
    }

    [Fact]
    public void AStoreWithNoRowsHasNoWorkingArea()
    {
        Store().WorkingPaths.ShouldBeEmpty();
    }

    [Fact]
    public void TheCommandThatWritesTheRowsNamesTheirTable()
    {
        // The save cascade finds a child's command by its configuration type (ByName is a stub on this collection).
        ConfigurationCommands.All().Single(command => command.ConfigType == typeof(DataStoreWorkingPathConfiguration)).ContainerName.ShouldBe("DataStoreWorkingPath");
    }
}
