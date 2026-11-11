using System.Linq;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// A data set's own filters are mapped from a create or update request as stated and checked before anything is written (FDW-4997): one the
/// data set cannot keep is refused with its code, where before every filter was accepted and dropped.
/// </summary>
public sealed class DataSetSavedFiltersTests
{
    private static DataSetFilterConditionPayload Payload(string field, string op, string? value, string dataType) =>
        new() { FieldName = field, Operator = op, Value = value, DataType = dataType };

    private static SimpleDataSetConfiguration Orders(params DataSetFilterConditionPayload[] filters) => new()
    {
        Name = "Orders",
        Implementation = "Simple",
        Fields = [new DataSetFieldConfiguration { Name = "Status", Ordinal = 0 }, new DataSetFieldConfiguration { Name = "Quantity", Ordinal = 1 }],
        Filters = DataSetQueryHelper.MapFilters(filters),
    };

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public void TheRequestsFiltersAreMappedAsStatedInTheirOrder()
    {
        var mapped = DataSetQueryHelper.MapFilters([Payload("Status", "Equal", "open", "String"), Payload("Quantity", "GreaterThan", "3", "Int32")]);

        mapped.Select(f => (f.FieldName, f.Operator, f.Value, f.DataType, f.Ordinal))
            .ShouldBe([("Status", "Equal", "open", "String", 0), ("Quantity", "GreaterThan", "3", "Int32", 1)]);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public void ADataSetWhoseFiltersCanBeKeptIsSaved()
    {
        DataSetQueryHelper.ValidateSaved(Orders(Payload("Status", "Equal", "open", "String")), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Category", "DataSets.FDW4997")]
    [InlineData("Region", "Equal", "north", "String", "DATASETS-20022")]
    [InlineData("Status", "Equals", "open", "String", "DATASETS-20023")]
    [InlineData("Quantity", "Equal", "many", "Int32", "DATASETS-20027")]
    public void AFilterTheDataSetCannotKeepIsRefusedBeforeItIsSaved(string field, string op, string value, string dataType, string code)
    {
        var refused = DataSetQueryHelper.ValidateSaved(Orders(Payload(field, op, value, dataType)), NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Code!.Code.ShouldBe(code);
    }

    private static FederatedDataSetConfiguration CustomerOrders(string joinType) => new()
    {
        Name = "CustomerOrders",
        Implementation = "Federated",
        FederationStrategy = "Sequential",
        Sources = [new DataSetSourceConfiguration { Name = "Orders", SourceName = "Orders" }, new DataSetSourceConfiguration { Name = "Customers", SourceName = "Customers" }],
        Joins =
        [
            new DataSetJoinConfiguration
            {
                LeftSourceName = "Orders", LeftFieldName = "CustomerId", RightSourceName = "Customers", RightFieldName = "CustomerId", JoinType = joinType, Ordinal = 1,
            },
        ],
    };

    [Theory]
    [Trait("Category", "DataSets.FDW5087")]
    [InlineData("Inner")]
    [InlineData("Left")]
    [InlineData("Right")]
    [InlineData("Full")]
    [InlineData("Cross")]
    public void AFederatedDataSetJoiningByATypeItPerformsIsSaved(string joinType)
    {
        DataSetQueryHelper.ValidateSaved(CustomerOrders(joinType), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5087")]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("None", "DATASETS-20028")]
    [InlineData("Outer", "DATASETS-20029")]
    [InlineData("", "DATASETS-20030")]
    public void AFederatedDataSetJoiningByATypeThatDoesNotJoinIsRefusedBeforeItIsSaved(string joinType, string code)
    {
        var refused = DataSetQueryHelper.ValidateSaved(CustomerOrders(joinType), NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Code!.Code.ShouldBe(code);
    }
}
