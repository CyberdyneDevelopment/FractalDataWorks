using System;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests.Categories;

/// <summary>
/// Tests for how a thing that sits under a category meets the tree of its category type (FDW-1466): a request names a category by its
/// stable name, an answer carries the name and path, a name that is not a category is refused naming the type, and a citation of a
/// category that is gone is a fault.
/// </summary>
public sealed class CategoryLinksTests
{
    private static readonly Guid Finance = Guid.CreateVersion7();
    private static readonly Guid Revenue = Guid.CreateVersion7();

    private static CategoryTree Tree()
    {
        var tree = CategoryTree.TryBuild(
            [
                new CategoryNode(Finance, null, "finance", "Finance", null, "Glossary"),
                new CategoryNode(Revenue, Finance, "revenue", "Revenue", null, "Glossary"),
            ],
            out var problems);
        problems.ShouldBeEmpty();
        return tree.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANameThatIsACategoryResolvesAndNoNameMeansNone()
    {
        CategoryLinks.Resolve(Tree(), "Glossary", "revenue", NullLogger.Instance).Value.ShouldBe(Revenue);
        CategoryLinks.Resolve(Tree(), "Glossary", null, NullLogger.Instance).Value.ShouldBeNull();
        CategoryLinks.Resolve(Tree(), "Glossary", "", NullLogger.Instance).Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AWhitespaceNameIsRefusedNotTakenAsNone()
    {
        CategoryLinks.Resolve(Tree(), "Glossary", "   ", NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUnknownNameIsRefusedAndNamesTheType()
    {
        var refused = CategoryLinks.Resolve(Tree(), "Glossary", "weather", NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        var message = refused.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("weather");
        message.ShouldContain("Glossary");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACitedCategoryIsLabelledWithItsNameAndPathAndAGoneOneIsAFault()
    {
        var label = CategoryLinks.Label(Tree(), "Glossary", Revenue, NullLogger.Instance);

        label.Value.ShouldBe(new CategoryLabel("revenue", "Finance / Revenue"));
        CategoryLinks.Label(Tree(), "Glossary", null, NullLogger.Instance).Value.ShouldBe(new CategoryLabel(null, null));
        CategoryLinks.Label(Tree(), "Glossary", Guid.CreateVersion7(), NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANameMoreThanOneTenantHoldsIsRefusedBecauseItDoesNotSayWhichButAReadByIdStillWorks()
    {
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var tree = CategoryTree.TryBuild(
            [
                new CategoryNode(first, null, "finance", "Finance", null, "Glossary", Guid.CreateVersion7()),
                new CategoryNode(second, null, "finance", "Money", null, "Glossary", Guid.CreateVersion7()),
            ],
            out var problems);
        problems.ShouldBeEmpty();
        tree.ShouldNotBeNull();

        var refused = CategoryLinks.Resolve(tree, "Glossary", "finance", NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("more than one tenant or organisation");
        CategoryLinks.Label(tree, "Glossary", second, NullLogger.Instance).Value.ShouldBe(new CategoryLabel("finance", "Money"));
    }
}
