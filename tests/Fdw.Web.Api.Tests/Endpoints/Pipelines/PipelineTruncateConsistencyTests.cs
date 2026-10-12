using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="PipelineTruncateConsistency"/> (FDW-1110): the load mode is the one definition and Replace means
/// truncate then load; <c>truncateBeforeLoad</c> is only a consistency check, and is read back derived from the load mode.
/// </summary>
public sealed class PipelineTruncateConsistencyTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Replace", true)]
    [InlineData("Append", false)]
    [InlineData("Upsert", false)]
    [InlineData(null, false)]
    public void ItIsDerivedFromTheLoadModeAndTrueExactlyForReplace(string? mode, bool expected) =>
        PipelineTruncateConsistency.Derive(mode).ShouldBe(expected);

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    [InlineData(true, "Replace")]
    [InlineData(false, "Replace")]
    [InlineData(false, "Append")]
    [InlineData(false, "Upsert")]
    [InlineData(false, null)]
    [InlineData(null, "Replace")]
    [InlineData(null, "Append")]
    [InlineData(null, null)]
    public void AValueThatDoesNotContradictTheLoadModeOrNoValueIsAccepted(bool? stated, string? mode) =>
        PipelineTruncateConsistency.Check(stated, mode, NullLogger.Instance).ShouldBeNull();

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    [InlineData(true, "Append")]
    [InlineData(true, "Upsert")]
    [InlineData(true, null)]
    public void TrueBesideALoadModeThatDoesNotTruncateIsRefusedWithAReason(bool stated, string? mode)
    {
        var refusal = PipelineTruncateConsistency.Check(stated, mode, NullLogger.Instance);

        refusal.ShouldNotBeNull();
        refusal.Message.ShouldContain("truncateBeforeLoad");
        refusal.Message.ShouldContain(mode ?? "load steps");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Replace", true)]
    [InlineData("Append", false)]
    public void TheDetailReadAnswersTruncateBeforeLoadDerivedFromTheStoredLoadMode(string mode, bool expected)
    {
        var detail = PipelineDetailMapper.Map(new BatchCopyPipelineConfiguration { Name = "Load", Implementation = "BatchCopy", LoadMode = mode });

        detail.TruncateBeforeLoad.ShouldBe(expected);
    }
}
