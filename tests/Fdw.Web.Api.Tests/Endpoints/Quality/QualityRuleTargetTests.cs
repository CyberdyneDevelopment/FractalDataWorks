using Fdw.Services.Quality.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Quality;

/// <summary>
/// Tests for <see cref="QualityRuleTarget"/>: a quality check with no data set to read is refused with its reason instead of
/// being sent to a store worked out from a name (FDW-1116).
/// </summary>
public sealed class QualityRuleTargetTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARuleThatNamesNoDataSetIsRefusedWithTheReason(string? dataSetName)
    {
        var result = QualityRuleTarget.RequireDataSet("Quality rule 42", dataSetName, NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
        var message = result.CurrentMessage;
        message.ShouldNotBeNull();
        message.ShouldContain("Quality rule 42");
        message.ShouldContain("names no data set");
        message.ShouldContain("never guessed");
    }

    [Theory]
    [InlineData("Teams")]
    [InlineData("NFL_Players")]
    [InlineData("Orders")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADataSetNameIsAcceptedWhateverItIsCalled(string dataSetName)
    {
        var result = QualityRuleTarget.RequireDataSet("Quality rule 42", dataSetName, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }
}
