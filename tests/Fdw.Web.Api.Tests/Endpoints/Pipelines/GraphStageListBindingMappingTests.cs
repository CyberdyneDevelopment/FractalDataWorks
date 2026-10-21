using System.Linq;
using System.Text.Json;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// FDW-1745: a Distinct tool whose key columns come from a list run parameter is read with the binding kept on the component and no key
/// column stored in its stage, and written back with the binding where the key columns would be.
/// </summary>
public sealed class GraphStageListBindingMappingTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static GraphPayload Payload()
    {
        var once = new GraphComponentPayload { Name = "once", Kind = "Distinct" };
        once.Settings["distinct"] = Json("{\"parameter\":\"Keys\"}");
        once.Settings["batchSize"] = Json("100");
        return new GraphPayload
        {
            Name = "DistinctBound",
            IsEnabled = true,
            LinkCapacity = 4,
            Parameters = [new GraphParameterPayload { Name = "Keys", DataType = "String", IsRequired = true, IsList = true }],
            Components = [once],
        };
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABoundDistinctIsReadWithTheBindingAndNoKeyColumnStored()
    {
        var read = GraphPayloadReader.Read(Payload(), System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var component = read.Value!.Components.Single();
        component.SettingBindings.ShouldHaveSingleItem().SettingName.ShouldBe("distinct");
        component.SettingBindings[0].ParameterName.ShouldBe("Keys");
        component.Stages.ShouldHaveSingleItem().GroupByFields.ShouldBeEmpty();
        read.Value.Parameters.ShouldHaveSingleItem().IsList.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABoundDistinctIsWrittenBackAsTheBindingWhereTheKeyColumnsWouldBe()
    {
        var read = GraphPayloadReader.Read(Payload(), System.Guid.NewGuid());

        var written = GraphPayloadWriter.Write(read.Value!).Components.Single();

        written.Settings["distinct"].ShouldBeOfType<System.Collections.Generic.Dictionary<string, object?>>()["parameter"].ShouldBe("Keys");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABoundSortIsReadWithTheBindingAndNoFieldStoredAndWrittenBackAsTheBinding()
    {
        var ordered = new GraphComponentPayload { Name = "ordered", Kind = "Sort" };
        ordered.Settings["sort"] = Json("{\"parameter\":\"Order\"}");
        ordered.Settings["batchSize"] = Json("100");
        var payload = new GraphPayload
        {
            Name = "SortBound",
            IsEnabled = true,
            LinkCapacity = 4,
            Parameters = [new GraphParameterPayload { Name = "Order", DataType = "String", IsRequired = true, IsList = true }],
            Components = [ordered],
        };

        var read = GraphPayloadReader.Read(payload, System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var component = read.Value!.Components.Single();
        component.SettingBindings.ShouldHaveSingleItem().SettingName.ShouldBe("sort");
        component.Stages.ShouldHaveSingleItem().SortFields.ShouldBeEmpty();
        var written = GraphPayloadWriter.Write(read.Value).Components.Single();
        written.Settings["sort"].ShouldBeOfType<System.Collections.Generic.Dictionary<string, object?>>()["parameter"].ShouldBe("Order");
    }
}
