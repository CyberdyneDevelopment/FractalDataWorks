using System.Reflection;
using System.Text.Json;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="UpdatePipelineChanges"/> (FDW-1146): the one list of members an update may change, the rule that an
/// update states at least one of them and the refusal message that names them.
/// </summary>
public sealed class UpdatePipelineChangesTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static UpdatePipelineRequest Body(string json) => JsonSerializer.Deserialize<UpdatePipelineRequest>(json, Web)!;

    [Theory]
    [InlineData("SourceDataSet", """{ "sourceDataSet": "Orders" }""")]
    [InlineData("SourceDataStoreName", """{ "sourceDataStoreName": "Feeds" }""")]
    [InlineData("SourceDataStorePath", """{ "sourceDataStorePath": "orders/v1" }""")]
    [InlineData("SourceContainerName", """{ "sourceContainerName": "orders" }""")]
    [InlineData("DestinationDataSet", """{ "destinationDataSet": "OrdersCopy" }""")]
    [InlineData("DestinationDataStoreName", """{ "destinationDataStoreName": "Warehouse" }""")]
    [InlineData("DestinationDataStorePath", """{ "destinationDataStorePath": "orders/v1" }""")]
    [InlineData("DestinationContainerName", """{ "destinationContainerName": "orders" }""")]
    [InlineData("Description", """{ "description": "nightly copy" }""")]
    [InlineData("IsEnabled", """{ "isEnabled": false }""")]
    [InlineData("TruncateBeforeLoad", """{ "truncateBeforeLoad": true }""")]
    [InlineData("Transforms", """{ "transforms": [] }""")]
    [InlineData("LoadMode", """{ "loadMode": "Append" }""")]
    [InlineData("LoadOptions", """{ "loadOptions": { "keyColumns": ["Id"] } }""")]
    [InlineData("LoadSteps", """{ "loadSteps": [] }""")]
    [InlineData("RejectSteps", """{ "rejectSteps": [] }""")]
    [InlineData("ValueSources", """{ "valueSources": [] }""")]
    [InlineData("Parameters", """{ "parameters": [] }""")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryMemberOnTheListChangesTheSavedPipelineOnItsOwn(string member, string json)
    {
        var body = Body(json);

        UpdatePipelineChanges.Stated(body).ShouldBeTrue();
        UpdatePipelineChanges.Stated(body, member).ShouldBeTrue();
        UpdatePipelineChanges.Names.ShouldContain(member);
    }

    [Theory]
    [InlineData("""{ }""")]
    [InlineData("""{ "name": "Nightly" }""")]
    [InlineData("""{ "rejectSteps": null }""")]
    [InlineData("""{ "runRoles": ["Admin"] }""")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesNoMemberChangesNothing(string json)
    {
        UpdatePipelineChanges.Stated(Body(json)).ShouldBeFalse();
    }

    // FDW-1154: a reject step list alone is a change, whether it replaces the stored steps (a list), removes them (an empty
    // list) or is absent (not stated).
    [Theory]
    [InlineData("""{ "rejectSteps": [] }""", true)]
    [InlineData("""{ "rejectSteps": [ { "order": 1, "name": "Keep rejects", "isEnabled": true, "kind": "BulkInsert", "destinationDataSet": "Rejects", "options": { "writeColumns": ["RejectReason", "RejectStage"] }, "valueSources": [] } ] }""", true)]
    [InlineData("""{ "rejectSteps": null }""", false)]
    [InlineData("""{ "description": "x" }""", false)]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARejectStepListIsAChangeWhenItIsEmptyOrFullAndNotWhenItIsAbsent(string json, bool changes)
    {
        var body = Body(json);

        UpdatePipelineChanges.Stated(body, nameof(UpdatePipelineRequest.RejectSteps)).ShouldBe(changes);
        PipelineLoadPatch.Changes(body).ShouldBe(changes);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheRefusalNamesEveryMemberTheListHolds()
    {
        foreach (var member in UpdatePipelineChanges.Names)
        {
            UpdatePipelineChanges.RefusalMessage.ShouldContain(member);
        }

        UpdatePipelineChanges.RefusalMessage.ShouldContain("RejectSteps");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryMemberOfTheRequestExceptTheNameIsOnTheList()
    {
        var statable = typeof(UpdatePipelineRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => !string.Equals(property.Name, nameof(UpdatePipelineRequest.Name), StringComparison.Ordinal))
            .Select(property => property.Name)
            .ToList();

        statable.ShouldBe(UpdatePipelineChanges.Names, ignoreOrder: true);
    }

    // FDW-1154: the body a proxy forwards is the client request. A member the server request has and the client request lacks
    // (or the reverse) is dropped on the way, so the two carry exactly the same members, the name aside (it is the route's).
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheClientRequestAndTheServerRequestCarryTheSameMembers()
    {
        static IEnumerable<string> Members(Type type) => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Where(name => !string.Equals(name, nameof(UpdatePipelineRequest.Name), StringComparison.Ordinal));

        Members(typeof(UpdatePipelineClientRequest)).ShouldBe(Members(typeof(UpdatePipelineRequest)), ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheLoadMembersAgreeWithTheLoadPatchRule()
    {
        foreach (var json in new[]
        {
            """{ "loadMode": "Append" }""",
            """{ "loadOptions": { "keyColumns": ["Id"] } }""",
            """{ "loadSteps": [] }""",
            """{ "rejectSteps": [] }""",
            """{ "valueSources": [] }""",
            """{ "parameters": [] }""",
            """{ "description": "x" }""",
        })
        {
            var body = Body(json);
            var loadMember = PipelineLoadPatch.Changes(body);

            (UpdatePipelineChanges.Stated(body) || loadMember).ShouldBeTrue();
            if (loadMember)
            {
                UpdatePipelineChanges.Stated(body).ShouldBeTrue();
            }
        }
    }
}
