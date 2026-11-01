using System;
using Fdw.Operations.Endpoints.Executions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Tests for <see cref="ExecutionListItemType"/> (FDW-2033): a list with no stated item type holds runs only, a stage is listed only when asked for,
/// a root or a correlation id asks for its whole family, and an item type nothing matches is refused.
/// </summary>
public sealed class ExecutionListItemTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AListWithNoItemTypeHoldsRunsOnly()
    {
        var chosen = ExecutionListItemType.Of(null, hasRoot: false, hasCorrelation: false, out var refusal);

        refusal.ShouldBeNull();
        chosen.ShouldNotBeNull().Name.ShouldBe("Job");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Stage")]
    [InlineData("stage")]
    [InlineData("Test")]
    public void AStatedItemTypeIsListedWhateverItIs(string itemType)
    {
        var chosen = ExecutionListItemType.Of(itemType, hasRoot: false, hasCorrelation: false, out var refusal);

        refusal.ShouldBeNull();
        string.Equals(chosen.ShouldNotBeNull().Name, itemType, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ARootOrACorrelationIdAsksForTheWholeFamily(bool hasRoot, bool hasCorrelation)
    {
        var chosen = ExecutionListItemType.Of(" ", hasRoot, hasCorrelation, out var refusal);

        refusal.ShouldBeNull();
        chosen.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnItemTypeNothingMatchesIsRefusedNamingTheTypes()
    {
        var chosen = ExecutionListItemType.Of("Stages", hasRoot: false, hasCorrelation: false, out var refusal);

        chosen.ShouldBeNull();
        refusal.ShouldNotBeNull().ShouldContain("Stages");
        refusal.ShouldContain("Job");
    }
}
