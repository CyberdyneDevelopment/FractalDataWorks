using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// A schedule names its pipeline by id, resolved as the person saving the schedule sees it (FDW-1088).
/// </summary>
public sealed class SchedulePipelineTests
{
    private static readonly Guid PipelineId = new("33333333-3333-3333-3333-333333333333");

    private readonly Mock<IPipelineClient> _pipelines = new();

    private void Answers(IGenericResult<PipelineDetailResponse> result)
        => _pipelines.Setup(p => p.Get(PipelineId, It.IsAny<CancellationToken>())).ReturnsAsync(result);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APipelineTheCallerCanSeeIsResolvedById()
    {
        Answers(GenericResult<PipelineDetailResponse>.Success(new PipelineDetailResponse { Id = PipelineId, Name = "Nightly", PipelineType = "Graph" }));

        var result = await SchedulePipeline.Resolve(_pipelines.Object, "NightlyAt2", PipelineId,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Nightly");
        _pipelines.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APipelineTheCallerCannotSeeIsRefusedAsNotFound()
    {
        var missing = new Mock<IResultCode>();
        missing.SetupGet(c => c.Id).Returns(30001);
        missing.SetupGet(c => c.Code).Returns("TEST-30001");
        missing.SetupGet(c => c.Domain).Returns("Test");
        missing.SetupGet(c => c.Severity).Returns(ResultSeverities.ByName("Error"));
        missing.Setup(c => c.FormatMessage(It.IsAny<IResultDetails?>())).Returns("The pipeline was not found.");
        Answers(GenericResult<PipelineDetailResponse>.Failure(missing.Object));

        var result = await SchedulePipeline.Resolve(_pipelines.Object, "NightlyAt2", PipelineId,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeSameAs(missing.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ALookupThatCouldNotBeMadeIsPassedOnAsItIs()
    {
        var failed = GenericResult<PipelineDetailResponse>.Failure(Mock.Of<IGenericMessage>());
        Answers(failed);

        var result = await SchedulePipeline.Resolve(_pipelines.Object, "NightlyAt2", PipelineId,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(failed);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ALookupThatSucceedsWithNoPipelineIsAFailure()
    {
        Answers(GenericResult<PipelineDetailResponse>.Success(null!));

        var result = await SchedulePipeline.Resolve(_pipelines.Object, "NightlyAt2", PipelineId,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
