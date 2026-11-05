using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for checking the level an owner states before it is written (FDW-3161, FDW-3975): the mode is stated, is an option, and is one the owner can state. The thresholds
/// are attached through the threshold routes and are not part of the level.
/// </summary>
public sealed class WaitThresholdValidatorTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("Server", "Own")]
    [InlineData("Server", "None")]
    [InlineData("Pipeline", "Inherit")]
    [InlineData("Pipeline", "Own")]
    [InlineData("Schedule", "None")]
    public void AModeTheOwnerCanStateIsAccepted(string owner, string mode)
    {
        WaitThresholdValidator.Check(owner, new WaitThresholdLevelPayload { Mode = mode }, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("Server", null, "does not state its mode")]
    [InlineData("Server", "", "does not state its mode")]
    [InlineData("Pipeline", "Sometimes", "Sometimes")]
    [InlineData("Server", "Inherit", "cannot be stated by a Server")]
    public void AModeThatIsMissingUnknownOrNotTheOwnersToStateIsRefusedNamingIt(string owner, string? mode, string reason)
    {
        var judged = WaitThresholdValidator.Check(owner, new WaitThresholdLevelPayload { Mode = mode }, NullLogger.Instance);

        judged.IsFailure.ShouldBeTrue();
        judged.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
    }
}
