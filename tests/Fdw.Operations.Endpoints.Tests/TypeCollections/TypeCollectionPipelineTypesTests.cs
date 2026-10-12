using System.Collections.Generic;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Etl.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// Tests that a stage kind which runs on only some pipeline types states them in the type-collection value summary
/// (FDW-1120 phase 4), as data the console reads, and that every other option states none.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class TypeCollectionPipelineTypesTests
{
    private sealed class BatchOnlyOption : ITypeOption, IStagePipelineTypesShape
    {
        public string Name => "BatchOnly";

        public string Category => "Combine";

        public IReadOnlyList<string>? PipelineTypes => ["BatchCopy"];
    }

    private sealed class EveryPipelineTypeOption : ITypeOption, IStagePipelineTypesShape
    {
        public string Name => "Anywhere";

        public string Category => "Combine";

        public IReadOnlyList<string>? PipelineTypes => null;
    }

    private sealed class PlainOption : ITypeOption
    {
        public string Name => "Plain";

        public string Category => "Combine";
    }

    [Fact]
    public void AKindThatRunsOnSomePipelineTypesStatesThem()
    {
        var summary = TypeCollectionResolver.ToSummary(new BatchOnlyOption());

        summary.PipelineTypes.ShouldBe(new[] { "BatchCopy" });
    }

    [Fact]
    public void AKindThatRunsOnEveryPipelineTypeStatesNone()
    {
        var summary = TypeCollectionResolver.ToSummary(new EveryPipelineTypeOption());

        summary.PipelineTypes.ShouldBeNull();
    }

    [Fact]
    public void AnOptionThatIsNotAStageKindStatesNone()
    {
        var summary = TypeCollectionResolver.ToSummary(new PlainOption());

        summary.PipelineTypes.ShouldBeNull();
    }

    [Fact]
    public void ThePipelineTypesAreListedAsPipelineTypesInTheJson()
    {
        var json = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new BatchOnlyOption()), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"pipelineTypes\":[\"BatchCopy\"]");
    }
}
