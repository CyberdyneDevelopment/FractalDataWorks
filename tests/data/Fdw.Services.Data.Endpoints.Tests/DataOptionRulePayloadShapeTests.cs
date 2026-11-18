using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Data.Clients.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6348 (stage 1 of FDW-6339): every request and answer shape of the data option rule routes serializes with exactly the fields
/// the contract states, so a field added, dropped or renamed is a contract change that fails here first.
/// </summary>
public sealed class DataOptionRulePayloadShapeTests
{
    public static TheoryData<object, string[]> Shapes => new()
    {
        { new DataOptionCustodianPayload(), ["userId", "name"] },
        { new DataOptionLeafPayload(), ["method", "minValues", "maxValues"] },
        { new DataOptionLimitsPayload(), ["rule", "value", "members"] },
        { new DataOptionMemberChangePayload(), ["member", "memberDisplayName", "old", "new"] },
        { new DataOptionMemberPayload(), ["name", "displayName", "of", "clearable"] },
        { new DataOptionNamedPayload(), ["name", "displayName"] },
        { new DataOptionOwnerBody(), ["kind", "id"] },
        { new DataOptionOwnerPayload(), ["kind", "id", "name"] },
        { new DataOptionPathCheckBody(), ["path", "method"] },
        { new DataOptionPathCheckPayload(), ["path", "method", "family", "minValues", "maxValues"] },
        { new DataOptionRuleChangeBody(), ["name", "abbreviation", "description", "column", "path", "method", "owner", "custodianUserId", "requiredPermission", "clear"] },
        { new DataOptionRuleChangePayload(), ["at", "byUserId", "byName", "ruleId", "ruleName", "kind", "kindDisplayName", "changes"] },
        { new DataOptionRulePayload { Owner = new() }, ["id", "name", "abbreviation", "description", "dataStore", "dataPath", "container", "column", "path", "method", "values", "owner", "custodianUserId", "custodianName", "custodianState", "requiredPermission", "active", "removed", "changedAt", "changedByUserId", "changedBy", "canChange"] },
        { new DataOptionRuleSwitchBody(), ["active"] },
        { new DataOptionTreeNodePayload(), ["name", "displayName", "description", "children", "leaf"] },
        { new DataOptionCustodianStatePayload(), ["name", "displayName", "needsAttention"] },
        { new DataOptionTreePayload { Root = new() }, ["root", "valueKinds", "ownerKinds", "custodianStates"] },
        { new DataOptionValueKindPayload(), ["name", "displayName", "member"] },
        { new DataOptionOwnerChoicePayload(), ["id", "name"] },
        { new DataOptionValueBody(), ["name", "abbreviation", "kind", "value", "parameter", "description", "ordinal"] },
        { new DataOptionValueChangeBody(), ["name", "abbreviation", "kind", "value", "parameter", "description", "ordinal", "clear"] },
        { new DataOptionValuePayload(), ["id", "name", "abbreviation", "kind", "value", "parameter", "description", "ordinal"] },
        { new DataOptionValuesBody(), ["values"] },
        { new NewDataOptionRuleBody(), ["name", "abbreviation", "description", "dataStore", "dataPath", "container", "column", "path", "method", "owner", "custodianUserId", "requiredPermission", "active", "values"] },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    [Trait("Priority", "P1")]
    public void TheShapeHasExactlyTheContractsFields(object shape, string[] fields)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(shape, shape.GetType(), JsonSerializerOptions.Web));

        json.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(fields, ignoreOrder: true);
    }
}
