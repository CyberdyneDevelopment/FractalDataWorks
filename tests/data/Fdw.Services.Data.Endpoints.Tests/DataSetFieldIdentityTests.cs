using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6200: a data set's field is identified by its name within the data set, so an update that sends the fields again keeps each stored field's Id
/// (a source and a field mapping already do), a new field is left for the save cascade to mint, and a field the request leaves out is not carried. Without
/// that every PATCH that sends <c>fields</c> gives every field a new Id and nothing can cite a field across saves.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataIntegrity")]
public sealed class DataSetFieldIdentityTests
{
    private static DataSetRequestValues Values() => new("Orders", NullLogger.Instance);

    private static CreateDataSetFieldRequest Field(string name) => new() { Name = name, DataType = "Int32", IsFilterable = true, IsNullable = false };

    [Fact]
    public void AFieldTheDataSetHasKeepsItsId()
    {
        var stored = new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Total", IsNullable = false };

        var mapped = DataSetQueryHelper.MapFields([Field("Total")], [stored], Values());

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value!.Single().Id.ShouldBe(stored.Id);
    }

    [Fact]
    public void ANewFieldHasNoIdSoTheSaveCascadeMintsOne()
    {
        var stored = new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Total", IsNullable = false };

        var mapped = DataSetQueryHelper.MapFields([Field("Total"), Field("Region")], [stored], Values());

        mapped.Value!.Single(f => f.Name == "Region").Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void OnCreateNoFieldHasAnId()
    {
        var mapped = DataSetQueryHelper.MapFields([Field("Total"), Field("Region")], [], Values());

        mapped.Value!.ShouldAllBe(f => f.Id == Guid.Empty);
    }

    [Fact]
    public void AStoredFieldTheRequestLeavesOutIsNotCarried()
    {
        var kept = new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Total", IsNullable = false };
        var dropped = new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Old", IsNullable = false };

        var mapped = DataSetQueryHelper.MapFields([Field("Total")], [kept, dropped], Values());

        mapped.Value!.Select(f => f.Id).ShouldBe([kept.Id]);
    }

    [Fact]
    public void TwoStoredFieldsKeepTheirOwnIdsWhateverOrderTheRequestSendsThem()
    {
        var total = new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Total", IsNullable = false };
        var region = new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Region", IsNullable = false };

        var mapped = DataSetQueryHelper.MapFields([Field("Region"), Field("Total")], [total, region], Values());

        mapped.Value!.Single(f => f.Name == "Total").Id.ShouldBe(total.Id);
        mapped.Value!.Single(f => f.Name == "Region").Id.ShouldBe(region.Id);
    }

    [Fact]
    public void AFieldTheDataSetHasKeepsWhatTheRequestHasNoMemberFor()
    {
        var container = Guid.NewGuid();
        var stored = new DataSetFieldConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Total",
            IsNullable = false,
            BoundContainerFieldId = container,
            BindingKind = "Discovered",
            BindingConfidence = 90,
            IsSystemProvided = true,
        };

        var field = DataSetQueryHelper.MapFields([Field("Total")], [stored], Values()).Value!.Single();

        field.BoundContainerFieldId.ShouldBe(container);
        field.BindingKind.ShouldBe("Discovered");
        field.BindingConfidence.ShouldBe((byte)90);
        field.IsSystemProvided.ShouldBeTrue();
    }

    [Fact]
    public void ANewFieldHasNoBindingAndIsNotSystemProvided()
    {
        var field = DataSetQueryHelper.MapFields([Field("Total")], [], Values()).Value!.Single();

        field.BoundContainerFieldId.ShouldBeNull();
        field.BindingKind.ShouldBeNull();
        field.BindingConfidence.ShouldBeNull();
        field.IsSystemProvided.ShouldBeFalse();
    }
}
