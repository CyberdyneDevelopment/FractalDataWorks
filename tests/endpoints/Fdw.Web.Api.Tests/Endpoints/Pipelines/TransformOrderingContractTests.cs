using System.Text.Json;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for how a transform request states Sort, LatestBy and Distinct (FDW-1110, contract v1 item 2): the contract's
/// <c>sort</c>, <c>latestBy</c> and <c>distinct</c> bodies reach the engine's spec as order fields, an order field and
/// a keep rule, with the key columns of LatestBy and Distinct in the group-by list FDW-1109 reads them from.
/// </summary>
public sealed class TransformOrderingContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static CreatePipelineTransformRequest Transform(string json) => JsonSerializer.Deserialize<CreatePipelineTransformRequest>(json, Web)!;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASortBodyReachesTheSpecAsOrderedSortFields()
    {
        var request = Transform("""{ "name": "Order", "operationType": "Sort", "sort": [ { "field": "Name", "direction": "Ascending" }, { "field": "Id", "direction": "Descending" } ] }""");

        var fields = ((ITransformOrderingSpec)request).SortFields;

        fields.Select(f => (f.FieldName, f.Direction)).ShouldBe([("Name", "Ascending"), ("Id", "Descending")]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ALatestByBodyReachesTheSpecAsKeyFieldsAnOrderFieldAndAKeepRule()
    {
        var request = Transform("""{ "name": "Newest", "operationType": "LatestBy", "latestBy": { "keyColumns": ["CustomerId"], "orderColumn": "UpdatedAt", "keep": "Last" } }""");

        ((ITransformOperationSpec)request).GroupByFields.ShouldBe(["CustomerId"]);
        ((ITransformOrderingSpec)request).OrderField.ShouldBe("UpdatedAt");
        ((ITransformOrderingSpec)request).KeepRule.ShouldBe("Last");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADistinctBodyReachesTheSpecAsKeyFieldsAndNoOrdering()
    {
        var request = Transform("""{ "name": "Unique", "operationType": "Distinct", "distinct": { "keyColumns": ["Email"] } }""");

        ((ITransformOperationSpec)request).GroupByFields.ShouldBe(["Email"]);
        ((ITransformOrderingSpec)request).OrderField.ShouldBeNull();
        ((ITransformOrderingSpec)request).KeepRule.ShouldBeNull();
        ((ITransformOrderingSpec)request).SortFields.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnAggregateStillGroupsByItsAggregationFields()
    {
        var request = Transform("""{ "name": "Totals", "operationType": "Aggregate", "aggregation": { "groupByFields": ["Region"], "aggregations": [] } }""");

        ((ITransformOperationSpec)request).GroupByFields.ShouldBe(["Region"]);
    }
}
