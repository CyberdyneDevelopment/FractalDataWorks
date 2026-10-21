using Fdw.Data.Abstractions;

namespace Fdw.Data.Abstractions.Tests.Categories;

/// <summary>
/// Tests for the category tree (FDW-1465): the paths, the nodes under a node, and the checks a stored set of nodes and a move must
/// pass.
/// </summary>
public sealed class CategoryTreeTests
{
    private static readonly Guid Sports = Guid.CreateVersion7();
    private static readonly Guid Football = Guid.CreateVersion7();
    private static readonly Guid Nfl = Guid.CreateVersion7();
    private static readonly Guid Weather = Guid.CreateVersion7();

    private static List<CategoryNode> Sample() =>
    [
        new(Nfl, Football, "nfl", "NFL", "Pro football", "DataSet"),
        new(Sports, null, "sports", "Sports", null, "DataSet"),
        new(Football, Sports, "football", "Football", null, "DataSet"),
        new(Weather, null, "weather", "Weather", null, "DataSet"),
    ];

    private static CategoryTree Build(IEnumerable<CategoryNode> nodes)
    {
        var tree = CategoryTree.TryBuild(nodes, out var problems);
        problems.ShouldBeEmpty();
        return tree.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANodePathIsTheDisplayNamesFromTheTopDown()
    {
        var tree = Build(Sample());

        tree.Find(Nfl).ShouldNotBeNull().Path.ShouldBe("Sports / Football / NFL");
        tree.FindByName("weather").ShouldNotBeNull().Path.ShouldBe("Weather");
        tree.Find(Guid.NewGuid()).ShouldBeNull();
        tree.FindByName("nothing").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheTopOfTheTreeAndEachLevelAreInDisplayNameOrder()
    {
        var tree = Build(Sample());

        tree.Roots.Select(root => root.Node.Name).ShouldBe(["sports", "weather"]);
        tree.Find(Sports).ShouldNotBeNull().Children.Select(child => child.Node.Name).ShouldBe(["football"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANodeAndEverythingUnderItAtAnyDepthIsWhatAFilterOnTheNodeIncludes()
    {
        var tree = Build(Sample());

        tree.SelfAndDescendants(Sports).ShouldBe([Sports, Football, Nfl]);
        tree.SelfAndDescendants(Nfl).ShouldBe([Nfl]);
        tree.SelfAndDescendants(Guid.NewGuid()).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMoveUnderItselfOrUnderANodeBelowItMakesACycle()
    {
        var tree = Build(Sample());

        tree.WouldMakeCycle(Sports, Sports).ShouldBeTrue();
        tree.WouldMakeCycle(Sports, Nfl).ShouldBeTrue();
        tree.WouldMakeCycle(Nfl, Sports).ShouldBeFalse();
        tree.WouldMakeCycle(Nfl, null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NodesThatDoNotMakeATreeAreNotBuiltAndEachProblemIsNamed()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        List<CategoryNode> nodes =
        [
            new(a, b, "a", "A", null, "DataSet"),
            new(b, a, "b", "B", null, "DataSet"),
            new(Guid.CreateVersion7(), Guid.CreateVersion7(), "orphan", "Orphan", null, "DataSet"),
            new(Guid.CreateVersion7(), null, "a", "Again", null, "DataSet"),
        ];

        var tree = CategoryTree.TryBuild(nodes, out var problems);

        tree.ShouldBeNull();
        problems.ShouldContain(problem => problem.Contains("'a' is used by more than one", StringComparison.Ordinal));
        problems.ShouldContain(problem => problem.Contains("'orphan' sits under a node that does not exist", StringComparison.Ordinal));
        problems.ShouldContain(problem => problem.Contains("sits under itself", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoTenantsMayEachHaveANodeOfTheSameNameAndEachKeepsItsOwnHierarchy()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var financeA = Guid.CreateVersion7();
        var loansA = Guid.CreateVersion7();
        var financeB = Guid.CreateVersion7();
        var tree = Build(
        [
            new(financeA, null, "finance", "Finance", null, "DataSet", tenantA),
            new(loansA, financeA, "loans", "Loans", null, "DataSet", tenantA),
            new(financeB, null, "finance", "Money", null, "DataSet", tenantB),
        ]);

        tree.Find(financeA).ShouldNotBeNull().Path.ShouldBe("Finance");
        tree.Find(financeB).ShouldNotBeNull().Path.ShouldBe("Money");
        tree.Find(loansA).ShouldNotBeNull().Path.ShouldBe("Finance / Loans");
        tree.FindAllByName("finance").Count.ShouldBe(2);
        tree.IsAmbiguous("finance").ShouldBeTrue();

        // The name alone does not say which, so it names none rather than guess.
        tree.FindByName("finance").ShouldBeNull();
        tree.IsAmbiguous("loans").ShouldBeFalse();
        tree.FindByName("loans").ShouldNotBeNull().Node.Id.ShouldBe(loansA);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANameUsedTwiceWithinOneTenantAndOrganisationIsStillRefused()
    {
        var tenant = Guid.CreateVersion7();
        var organisation = Guid.CreateVersion7();
        var tree = CategoryTree.TryBuild(
            [
                new CategoryNode(Guid.CreateVersion7(), null, "finance", "Finance", null, "DataSet", tenant, organisation),
                new CategoryNode(Guid.CreateVersion7(), null, "finance", "Money", null, "DataSet", tenant, organisation),
            ],
            out var problems);

        tree.ShouldBeNull();
        problems.ShouldContain(problem => problem.Contains("'finance' is used by more than one", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrganisationsOfOneTenantEachHoldTheirOwnHierarchyToo()
    {
        var tenant = Guid.CreateVersion7();
        var tree = Build(
        [
            new(Guid.CreateVersion7(), null, "ops", "Ops", null, "DataSet", tenant, Guid.CreateVersion7()),
            new(Guid.CreateVersion7(), null, "ops", "Ops", null, "DataSet", tenant, Guid.CreateVersion7()),
        ]);

        tree.IsAmbiguous("ops").ShouldBeTrue();
    }
}
