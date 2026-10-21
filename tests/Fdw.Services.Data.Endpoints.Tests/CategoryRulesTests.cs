using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// Tests for the data set category write rules and the way a node is answered (FDW-1465): a name a route can carry, a display name, a
/// parent that is a category, a move that makes no cycle, and a path with the parent named.
/// </summary>
public sealed class CategoryRulesTests
{
    private static readonly Guid Sports = Guid.CreateVersion7();
    private static readonly Guid Football = Guid.CreateVersion7();

    private static CategoryTree Tree()
    {
        var tree = CategoryTree.TryBuild(
            [
                new CategoryNode(Sports, null, "sports", "Sports", "All sports", "DataSet"),
                new CategoryNode(Football, Sports, "football", "Football", null, "DataSet"),
            ],
            out var problems);
        problems.ShouldBeEmpty();
        return tree.ShouldNotBeNull();
    }

    private static CategoryPayload Payload(string name = "nfl", string displayName = "NFL", string? parent = "football", string type = "DataSet") =>
        new() { Name = name, DisplayName = displayName, Parent = parent, Type = type };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACategoryUnderAnExistingParentIsAcceptedAndNamesTheParentsIdentity()
    {
        var parent = CategoryRules.Parent(Payload(), Tree());

        parent.IsSuccess.ShouldBeTrue(parent.CurrentMessage);
        parent.Value.ShouldBe(Football);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATopLevelCategoryHasNoParent()
    {
        var parent = CategoryRules.Parent(Payload(parent: null), Tree());

        parent.IsSuccess.ShouldBeTrue(parent.CurrentMessage);
        parent.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryProblemOfAWriteIsNamedAtOnce()
    {
        var refused = CategoryRules.Parent(Payload(name: "a/b", displayName: " ", parent: "nowhere"), Tree());

        refused.IsFailure.ShouldBeTrue();
        var message = refused.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("name");
        message.ShouldContain("display name");
        message.ShouldContain("nowhere");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACategoryUnderAParentOfAnotherTypeOrOfATypeThatDoesNotExistIsRefused()
    {
        var otherType = CategoryRules.Parent(Payload(type: "Custom"), Tree());
        var noType = CategoryRules.Parent(Payload(type: "Nothing"), Tree());

        otherType.IsFailure.ShouldBeTrue();
        otherType.CurrentMessage.ShouldNotBeNull().ShouldContain("cannot sit under it");
        noType.IsFailure.ShouldBeTrue();
        noType.CurrentMessage.ShouldNotBeNull().ShouldContain("Nothing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheTypesOfCategoryAreDataSetGlossaryAndCustomAndEachMakesItsOwnConfiguration()
    {
        CategoryTypes.All().Select(type => type.Name).ShouldBe(["Custom", "DataSet", "Glossary"], ignoreOrder: true);
        CategoryTypes.ByName("DataSet").NewConfiguration().ShouldBeOfType<DataSetCategoryConfiguration>();
        CategoryTypes.ByName("Custom").NewConfiguration().ShouldBeOfType<CustomCategoryConfiguration>();
        CategoryTypes.ByName("Glossary").NewConfiguration().ShouldBeOfType<GlossaryCategoryConfiguration>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMoveUnderACategoryBelowItIsRefused()
    {
        CategoryRules.Move(Tree(), Sports, Football).IsFailure.ShouldBeTrue();
        CategoryRules.Move(Tree(), Football, null).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACategoryIsAnsweredWithItsPathAndItsParentsName()
    {
        var tree = Tree();

        var payload = CategoryMapper.Category(tree, tree.FindByName("football").ShouldNotBeNull());

        payload.Path.ShouldBe("Sports / Football");
        payload.Parent.ShouldBe("sports");
        payload.Type.ShouldBe("DataSet");
        payload.Id.ShouldBe(Football);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheTreeIsAnsweredAsBranchesWithEverythingUnderEachTopLevelCategory()
    {
        var branch = CategoryMapper.Branch(Tree().Roots.Single());

        branch.Name.ShouldBe("sports");
        branch.Description.ShouldBe("All sports");
        branch.Children.Select(child => child.Path).ShouldBe(["Sports / Football"]);
    }

    private static CategoryTree TwoFinances(out Guid financeA, out Guid loansA)
    {
        financeA = Guid.CreateVersion7();
        loansA = Guid.CreateVersion7();
        var tree = CategoryTree.TryBuild(
            [
                new CategoryNode(financeA, null, "finance", "Finance", null, "DataSet", Guid.CreateVersion7()),
                new CategoryNode(loansA, financeA, "loans", "Loans", null, "DataSet", Guid.CreateVersion7()),
                new CategoryNode(Guid.CreateVersion7(), null, "finance", "Money", null, "DataSet", Guid.CreateVersion7()),
            ],
            out var problems);
        problems.ShouldBeEmpty();
        return tree.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParentNamedByMoreThanOneTenantIsRefusedForANewCategory()
    {
        var tree = TwoFinances(out _, out _);

        var parent = CategoryRules.Parent(Payload(name: "budgets", parent: "finance"), tree);

        parent.IsFailure.ShouldBeTrue();
        parent.CurrentMessage.ShouldNotBeNull().ShouldContain("more than one tenant or organisation");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnEditThatLeavesTheParentWhereItIsIsNotRefusedForTheParentsName()
    {
        var tree = TwoFinances(out var financeA, out var loansA);
        var current = tree.Find(loansA).ShouldNotBeNull().Node.ParentId;

        var parent = CategoryRules.Parent(Payload(name: "loans", displayName: "Loans, edited", parent: "finance"), tree, current);

        parent.IsSuccess.ShouldBeTrue(parent.CurrentMessage);
        parent.Value.ShouldBe(financeA);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACategoryIsAnsweredWithTheTenantAndOrganisationItBelongsTo()
    {
        var tenant = Guid.CreateVersion7();
        var group = Guid.CreateVersion7();
        var id = Guid.CreateVersion7();
        var tree = CategoryTree.TryBuild([new CategoryNode(id, null, "ops", "Ops", null, "DataSet", tenant, group)], out _).ShouldNotBeNull();

        var payload = CategoryMapper.Category(tree, tree.Find(id).ShouldNotBeNull());

        payload.TenantId.ShouldBe(tenant);
        payload.VisibilityGroupId.ShouldBe(group);
        CategoryMapper.Branch(tree.Find(id).ShouldNotBeNull()).TenantId.ShouldBe(tenant);
    }

    private static readonly Guid TenantA = Guid.CreateVersion7();
    private static readonly Guid TenantB = Guid.CreateVersion7();
    private static readonly Guid Global = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
    private static readonly Guid Group = Guid.CreateVersion7();

    private static (CategoryTree Tree, Guid Parent) TreeWith(Guid? tenant, Guid? group)
    {
        var parent = Guid.CreateVersion7();
        var tree = CategoryTree.TryBuild([new CategoryNode(parent, null, "finance", "Finance", null, "DataSet", tenant, group)], out _);
        return (tree.ShouldNotBeNull(), parent);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACategoryMayStandUnderAParentOfItsOwnTenant()
    {
        var (tree, parent) = TreeWith(TenantA, null);

        CategoryRules.Scope(tree, parent, TenantA, null).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACategoryMayStandUnderASharedParentFromAnyTenant()
    {
        var (tree, parent) = TreeWith(Global, null);

        CategoryRules.Scope(tree, parent, TenantA, null).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACategoryMayStandUnderASharedParentOfTheDefaultTenant()
    {
        var (tree, parent) = TreeWith(Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE"), null);

        CategoryRules.Scope(tree, parent, TenantA, null).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnOrganisationNarrowedParentOfASharedTenantIsNotShared()
    {
        var (tree, parent) = TreeWith(Global, Group);

        CategoryRules.Scope(tree, parent, TenantA, null).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASharedCategoryIsRefusedUnderAParentOfOneTenant()
    {
        var (tree, parent) = TreeWith(TenantA, null);

        var refused = CategoryRules.Scope(tree, parent, Global, null);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("another tenant or organisation");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACategoryIsRefusedUnderAParentOfAnotherTenant()
    {
        var (tree, parent) = TreeWith(TenantB, null);

        CategoryRules.Scope(tree, parent, TenantA, null).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AParentNarrowedToAnOrganisationDoesNotCoverACategoryOfTheWholeTenant()
    {
        var (tree, parent) = TreeWith(TenantA, Group);

        CategoryRules.Scope(tree, parent, TenantA, null).IsFailure.ShouldBeTrue();
        CategoryRules.Scope(tree, parent, TenantA, Group).IsSuccess.ShouldBeTrue();
        CategoryRules.Scope(tree, parent, Global, null).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ATopLevelCategoryNeedsNoParentScope()
    {
        var (tree, _) = TreeWith(TenantA, null);

        CategoryRules.Scope(tree, null, Global, null).IsSuccess.ShouldBeTrue();
    }
}
