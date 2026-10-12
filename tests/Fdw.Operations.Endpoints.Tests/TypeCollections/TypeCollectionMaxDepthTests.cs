using System.Text.Json;
using Fdw.Collections;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Etl.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// Tests that a stage kind which holds stages of its own states how deep they may nest in the type-collection value summary
/// (FDW-1120), as data the console reads, and that every other option states none.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class TypeCollectionMaxDepthTests
{
    private sealed class NestingOption : ITypeOption, INestedStageShape
    {
        public string Name => "Nesting";

        public string Category => "Routing";

        public int MaxDepth => 3;
    }

    private sealed class PlainOption : ITypeOption
    {
        public string Name => "Plain";

        public string Category => "Routing";
    }

    [Fact]
    public void AKindThatHoldsStagesStatesItsNestingLimit()
    {
        var summary = TypeCollectionResolver.ToSummary(new NestingOption());

        summary.MaxDepth.ShouldBe(3);
    }

    [Fact]
    public void AKindThatHoldsNoStagesStatesNoLimit()
    {
        var summary = TypeCollectionResolver.ToSummary(new PlainOption());

        summary.MaxDepth.ShouldBeNull();
    }

    [Fact]
    public void TheLimitIsListedAsMaxDepthInTheJson()
    {
        var json = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new NestingOption()), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"maxDepth\":3");
    }
}
