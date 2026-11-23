using System;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// A data set moves between kinds carrying everything it is (FDW-6806): its Id, name, fields, sources, keys, filters, category, owner and
/// version; a part the target kind has no place for is refused, never dropped.
/// </summary>
public sealed class DataSetKindMoveTests
{
    private static readonly Guid DataSetId = Guid.Parse("6806aaaa-0000-0000-0000-000000000001");
    private static readonly Guid FieldId = Guid.Parse("6806aaaa-0000-0000-0000-000000000002");
    private static readonly Guid CategoryId = Guid.Parse("6806aaaa-0000-0000-0000-000000000003");
    private static readonly Guid OwnerId = Guid.Parse("6806aaaa-0000-0000-0000-000000000004");

    private static SimpleDataSetConfiguration Simple() => new()
    {
        Id = DataSetId,
        Name = "Orders",
        Domain = "DataSet",
        Implementation = "Simple",
        DisplayName = "Orders shown",
        Version = "3",
        CategoryId = CategoryId,
        OwnerUserId = OwnerId,
        ClassificationId = 7,
        Fields = [new DataSetFieldConfiguration { Id = FieldId, Name = "OrderId" }],
        Sources = [new DataSetSourceConfiguration { Name = "Orders", SourceName = "Orders", DataStoreName = "Sales" }],
        KeyFields = [new DataSetKeyFieldConfiguration { KeyName = "OrderId", KeyType = "Surrogate", Ordinal = 0 }],
        Filters = [new DataSetFilterConditionConfiguration { FieldName = "OrderId", Operator = "GreaterThan", Value = "0", DataType = "Int32" }],
    };

    [Theory]
    [Trait("Category", "DataSets.FDW6806")]
    [InlineData("Compound")]
    [InlineData("Federated")]
    public void ASimpleDataSetMovesToAJoinedKindKeepingItsIdentityAndParts(string kind)
    {
        var moved = DataSetKindMove.To(Simple(), kind, NullLogger.Instance);

        moved.IsSuccess.ShouldBeTrue(moved.CurrentMessage);
        var target = moved.Value.ShouldNotBeNull();
        target.Implementation.ShouldBe(kind);
        target.ShouldBeAssignableTo<IJoinedDataSetConfiguration>();
        target.Id.ShouldBe(DataSetId);
        target.Name.ShouldBe("Orders");
        target.DisplayName.ShouldBe("Orders shown");
        target.Version.ShouldBe("3");
        target.CategoryId.ShouldBe(CategoryId);
        target.OwnerUserId.ShouldBe(OwnerId);
        target.ClassificationId.ShouldBe(7);
        target.Fields.ShouldHaveSingleItem().Id.ShouldBe(FieldId);
        target.Sources.ShouldHaveSingleItem().SourceName.ShouldBe("Orders");
        target.KeyFields.ShouldHaveSingleItem().KeyName.ShouldBe("OrderId");
        target.Filters.ShouldHaveSingleItem().FieldName.ShouldBe("OrderId");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public void AggregatesGoingToAJoinedKindAreRefusedNotDropped()
    {
        var simple = Simple();
        simple.Aggregates = [new DataSetAggregateConfiguration { AggregateColumnName = "Total" }];

        var moved = DataSetKindMove.To(simple, "Compound", NullLogger.Instance);

        moved.IsFailure.ShouldBeTrue();
        moved.Code!.Code.ShouldBe("DATASETS-20014");
    }

    [Theory]
    [Trait("Category", "DataSets.FDW6806")]
    [InlineData(true, null)]
    [InlineData(false, "Union")]
    public void JoinsOrAStatedCombinationGoingToSimpleAreRefusedNotDropped(bool joined, string? combination)
    {
        var compound = new CompoundDataSetConfiguration
        {
            Id = DataSetId,
            Name = "Orders",
            Implementation = "Compound",
            SourceCombination = combination,
            Joins = joined ? [new DataSetJoinConfiguration { LeftSourceName = "A", LeftFieldName = "Id", RightSourceName = "B", RightFieldName = "AId", JoinType = "Inner" }] : [],
        };

        var moved = DataSetKindMove.To(compound, "Simple", NullLogger.Instance);

        moved.IsFailure.ShouldBeTrue();
        moved.Code!.Code.ShouldBe("DATASETS-20013");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public void AFederatedDataSetMovingToCompoundKeepsItsJoinsAndStatement()
    {
        var federated = new FederatedDataSetConfiguration
        {
            Id = DataSetId,
            Name = "Orders",
            Implementation = "Federated",
            FederationStrategy = "Parallel",
            SourceCombination = "Join",
            Joins = [new DataSetJoinConfiguration { LeftSourceName = "A", LeftFieldName = "Id", RightSourceName = "B", RightFieldName = "AId", JoinType = "Left" }],
        };

        var moved = DataSetKindMove.To(federated, "Compound", NullLogger.Instance);

        moved.IsSuccess.ShouldBeTrue(moved.CurrentMessage);
        var compound = moved.Value.ShouldBeOfType<CompoundDataSetConfiguration>();
        compound.SourceCombination.ShouldBe("Join");
        compound.Joins.ShouldHaveSingleItem().JoinType.ShouldBe("Left");
    }
}
