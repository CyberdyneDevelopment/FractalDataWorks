using System.Linq;
using Fdw.Web.RestEndpoints.OpenApi;

namespace Fdw.Web.RestEndpoints.Tests.OpenApi;

/// <summary>
/// Every operation is listed under one group, the resource its URL names (FDW-1100). The cases are the pairs a live
/// reference listed twice before this rule: FastEndpoints' title-cased path tag beside the CRUD bases' own tag.
/// </summary>
public sealed class OperationTagsTests
{
    public static TheoryData<string, string[], string> Grouped => new()
    {
        { "/api/v1/datasets/{name}/fields/classification", ["datasets", "Datasets"], "Datasets" },
        { "/api/v1/calculation-entities", ["calculation-entities", "Calculation-Entities"], "Calculation Entities" },
        { "/api/v1/settings/server", ["settings/server", "Settings"], "Settings" },
        { "/api/v1/configuration/types/children", ["configuration-types", "Configuration"], "Configuration" },
        { "/api/v1/field-mappings/{fieldMappingId}/transforms/{transformationName}", ["field-mapping-transforms"], "Field Mappings" },
        { "/api/v1/connection-types/{name}/capabilities", ["Connection-Types"], "Connection Types" },
        { "/api/v1/auth/logout", ["Authentication", "Auth"], OperationTags.Authentication },
        { "/connect/token", [], OperationTags.Authentication },
        { "/endpoints/status", [], "Endpoints" },
    };

    [Theory]
    [MemberData(nameof(Grouped))]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnOperationIsListedUnderTheResourceItsUrlNames(string path, string[] declared, string expected)
        => OperationTags.For(path, declared).ShouldBe(expected);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARouteOfOnlyParametersTakesItsDeclaredGroupNotAParameterName()
        => OperationTags.For("/api/v1/{workspaceName}/{dataSetName}", ["workspaces"]).ShouldBe("Workspaces");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARouteOfOnlyParametersWithNoDeclaredGroupIsLeftUngrouped()
        => OperationTags.For("/api/v1/{workspaceName}/{dataSetName}", ["", " "]).ShouldBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SpellingsThatDifferOnlyInCaseOrSeparatorShowTheSame()
        => new[] { "data-views", "Data-Views", "data_views" }.Select(OperationTags.Display).Distinct().ShouldHaveSingleItem().ShouldBe("Data Views");
}
