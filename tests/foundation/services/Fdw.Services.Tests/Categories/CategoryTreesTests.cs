using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Categories;

/// <summary>
/// Tests for how the category tree is read from the domain table (FDW-1494): each row's tenant and organisation reach the node, so two
/// tenants that both hold 'finance' make a tree and an administrator who sees both is not told the store is unsound.
/// </summary>
public sealed class CategoryTreesTests
{
    private static ICategoryConfigurationProvider Holding(params CategoryDomainConfiguration[] rows)
    {
        var provider = new Mock<ICategoryConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<CategoryDomainConfiguration>>.Success(
                new ConfigurationPage<CategoryDomainConfiguration>(0, rows.Length + 1, false, _ => Stream(rows))));
        return provider.Object;
    }

    private static async IAsyncEnumerable<CategoryDomainConfiguration> Stream(IEnumerable<CategoryDomainConfiguration> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static CategoryDomainConfiguration Row(Guid id, string name, string display, Guid? tenant, Guid? parent = null) => new()
    {
        Id = id,
        Name = name,
        DisplayName = display,
        Implementation = "DataSet",
        ParentCategoryId = parent,
        TenantId = tenant,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TwoTenantsThatBothHoldANameMakeATreeWhoseNodesKeepTheirTenant()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var financeA = Guid.CreateVersion7();
        var financeB = Guid.CreateVersion7();

        var tree = await CategoryTrees.Load(
            Holding(Row(financeA, "finance", "Finance", tenantA), Row(financeB, "finance", "Money", tenantB)),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        tree.IsSuccess.ShouldBeTrue(tree.CurrentMessage);
        tree.Value!.Find(financeA).ShouldNotBeNull().Node.TenantId.ShouldBe(tenantA);
        tree.Value.Find(financeB).ShouldNotBeNull().Node.TenantId.ShouldBe(tenantB);
        tree.Value.IsAmbiguous("finance").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANameUsedTwiceInOneTenantIsStillAnUnsoundTree()
    {
        var tenant = Guid.CreateVersion7();

        var tree = await CategoryTrees.Load(
            Holding(Row(Guid.CreateVersion7(), "finance", "Finance", tenant), Row(Guid.CreateVersion7(), "finance", "Money", tenant)),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        tree.IsFailure.ShouldBeTrue();
        tree.Code.ShouldNotBeNull().Name.ShouldBe("CategoryTreeUnsound");
    }
}
