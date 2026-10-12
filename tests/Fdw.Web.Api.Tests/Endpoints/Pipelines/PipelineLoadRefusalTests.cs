using Fdw.Results;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="PipelineLoadRefusal"/> (FDW-1147): a refused load definition answers the caller 400 with the reason,
/// as <c>POST /pipelines/validate</c> lists it, instead of the generic text of its category.
/// </summary>
public sealed class PipelineLoadRefusalTests
{
    private const string Reason = "In reject step 'Keep rejects': the reject destination 'Breweries' is a destination the pipeline's load writes to";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARefusedLoadDefinitionAnswers400WithTheReason()
    {
        var (status, problem) = ResultHttpStatusMapper.Map(PipelineLoadRefusal.Of(Reason), new DefaultHttpContext());

        status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain(Reason);
    }
}
