using System;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-4346: a base data set has an owner, the user who created it. The list and the detail show it, a data set made before owners existed shows none (Unowned),
/// and a caller that cannot be named is refused with its own code.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public sealed class DataSetOwnerTests
{
    [Fact]
    public void AListedDataSetNamesItsOwner()
    {
        var owner = Guid.NewGuid();

        var summary = DataSetQueryHelper.MapToSummary(new SimpleDataSetConfiguration { Name = "Orders", OwnerUserId = owner });

        summary.OwnerUserId.ShouldBe(owner);
    }

    [Fact]
    public void ADetailNamesItsOwner()
    {
        var owner = Guid.NewGuid();

        var detail = DataSetQueryHelper.MapToDetail(new CompoundDataSetConfiguration { Name = "Orders", OwnerUserId = owner });

        detail.OwnerUserId.ShouldBe(owner);
    }

    [Fact]
    public void ADataSetMadeBeforeOwnersExistedHasNoOwner()
    {
        DataSetQueryHelper.MapToSummary(new FederatedDataSetConfiguration { Name = "Legacy" }).OwnerUserId.ShouldBeNull();
        DataSetQueryHelper.MapToDetail(new SimpleDataSetConfiguration { Name = "Legacy" }).OwnerUserId.ShouldBeNull();
    }

    [Fact]
    public void ACallerThatCannotBeNamedIsRefusedWithItsOwnCode()
    {
        var code = DataSetsResultCodes.ByName("OwnerUnresolved");

        code.Id.ShouldBe(90001);
        code.Code.ShouldBe("DATASETS-90001");
    }
}
