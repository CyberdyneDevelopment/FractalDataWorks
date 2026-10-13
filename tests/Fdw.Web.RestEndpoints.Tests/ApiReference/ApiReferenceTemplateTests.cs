using System.Text.Json.Nodes;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>
/// FDW-1047: OpenAPI treats two templated paths that differ only in their parameter names as one path, and a reader that
/// ignores case rejects a document that holds both. A reference document spells each template one way, and refuses two
/// templates it cannot honestly merge.
/// </summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class ApiReferenceTemplateTests
{
    private static ApiReferenceDocument Parse(string json) => ApiReferenceDocument.Parse(json, NullLogger.Instance).Value!;

    private static JsonNode Paths(ApiReferenceDocument document) => JsonNode.Parse(document.ToJson())!["paths"]!;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TwoSpellingsOfATemplateThatDifferOnlyInCaseBecomeOnePathWithOneParameterName()
    {
        var document = Parse("""
            {"openapi":"3.0.0","paths":{
              "/api/v1/field-mappings/{FieldMappingId}/transformations":{"get":{"parameters":[{"name":"FieldMappingId","in":"path","required":true}]}},
              "/api/v1/field-mappings/{fieldMappingId}/transformations":{"post":{"parameters":[{"name":"fieldMappingId","in":"path","required":true},{"name":"Q","in":"query"}]}}}}
            """);

        var result = document.UnifyPathTemplates(NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(1);
        document.Paths.ShouldBe(new[] { "/api/v1/field-mappings/{fieldMappingId}/transformations" });
        var item = Paths(document)["/api/v1/field-mappings/{fieldMappingId}/transformations"]!;
        item["get"]!["parameters"]![0]!["name"]!.GetValue<string>().ShouldBe("fieldMappingId");
        item["post"]!["parameters"]![0]!["name"]!.GetValue<string>().ShouldBe("fieldMappingId");
        item["post"]!["parameters"]![1]!["name"]!.GetValue<string>().ShouldBe("Q");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ATemplateThatHasNoSecondSpellingIsLeftAsItWas()
    {
        var document = Parse("""
            {"openapi":"3.0.0","paths":{
              "/a/{FieldMappingId}":{"get":{"parameters":[{"name":"FieldMappingId","in":"path"}]}},
              "/b/{x}":{"get":{}}}}
            """);

        var result = document.UnifyPathTemplates(NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
        document.Paths.ShouldBe(new[] { "/a/{FieldMappingId}", "/b/{x}" });
        Paths(document)["/a/{FieldMappingId}"]!["get"]!["parameters"]![0]!["name"]!.GetValue<string>().ShouldBe("FieldMappingId");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TemplatesWhoseParametersDifferByMoreThanCaseAreRefused()
    {
        var document = Parse("""
            {"openapi":"3.0.0","paths":{
              "/api/v1/users/{idOrName}":{"delete":{}},
              "/api/v1/users/{name}":{"patch":{}}}}
            """);

        document.UnifyPathTemplates(NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AnOperationDeclaredUnderTwoSpellingsOfOneTemplateIsRefused()
    {
        var document = Parse("""
            {"openapi":"3.0.0","paths":{
              "/a/{Id}":{"get":{"summary":"one"}},
              "/a/{id}":{"get":{"summary":"two"}}}}
            """);

        document.UnifyPathTemplates(NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ALiteralSegmentThatDiffersInCaseIsAnotherPathAndIsNotMerged()
    {
        var document = Parse("""{"openapi":"3.0.0","paths":{"/Users/{id}":{"get":{}},"/users/{id}":{"get":{}}}}""");

        var result = document.UnifyPathTemplates(NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
        document.Paths.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheMergedPathKeepsThePlaceOfTheFirstSpelling()
    {
        var document = Parse("""
            {"openapi":"3.0.0","paths":{"/a":{"get":{}},"/b/{X}":{"get":{}},"/c":{"get":{}},"/b/{x}":{"post":{}}}}
            """);

        document.UnifyPathTemplates(NullLogger.Instance).IsSuccess.ShouldBeTrue();

        document.Paths.ShouldBe(new[] { "/a", "/b/{x}", "/c" });
        var merged = Paths(document)["/b/{x}"]!.AsObject();
        merged.ContainsKey("get").ShouldBeTrue();
        merged.ContainsKey("post").ShouldBeTrue();
    }
}
