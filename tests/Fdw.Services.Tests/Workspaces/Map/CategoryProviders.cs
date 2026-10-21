using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Moq;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>Category providers for the map and lineage tests (FDW-1483): one that holds a tree, one that cannot be read.</summary>
internal static class CategoryProviders
{
    /// <summary>A provider whose domain table holds the given rows.</summary>
    public static ICategoryConfigurationProvider Holding(params CategoryDomainConfiguration[] rows)
    {
        var provider = new Mock<ICategoryConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<CategoryDomainConfiguration>>.Success(
                new ConfigurationPage<CategoryDomainConfiguration>(0, rows.Length + 1, false, _ => Stream(rows))));
        return provider.Object;
    }

    /// <summary>A provider that holds no categories.</summary>
    public static ICategoryConfigurationProvider Empty() => Holding();

    /// <summary>A provider whose read fails.</summary>
    public static ICategoryConfigurationProvider Failing(IGenericResult failure)
    {
        var provider = new Mock<ICategoryConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => failure.ToNewResult<ConfigurationPage<CategoryDomainConfiguration>>());
        return provider.Object;
    }

    /// <summary>A DataSet-type category row.</summary>
    public static CategoryDomainConfiguration Node(Guid id, Guid? parent, string name, string displayName) => new()
    {
        Id = id,
        Name = name,
        DisplayName = displayName,
        ParentCategoryId = parent,
        Implementation = DataSetCategoryConfiguration.ImplementationName,
    };

    private static async IAsyncEnumerable<CategoryDomainConfiguration> Stream(IEnumerable<CategoryDomainConfiguration> rows)
    {
        foreach (var row in rows.ToList())
        {
            yield return row;
        }

        await System.Threading.Tasks.Task.CompletedTask.ConfigureAwait(false);
    }
}
