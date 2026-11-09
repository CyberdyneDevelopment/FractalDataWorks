using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections.Results;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// A run refused because its connection is disabled is not counted by the failure thresholds (FDW-3951, FDW-4005): the code is in the Refused category, whose failures are the
/// state of something the run uses and not the fault of what ran.
/// </summary>
public sealed class ConnectionDisabledCodeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheRefusalIsInTheRefusedCategoryAndDoesNotCountTowardAFailureThreshold()
    {
        var failure = GenericResult.Failure(ConnectionsResultCodes.ByName("ConnectionDisabled"));

        var category = FailureCategory.Of(failure);

        category.ShouldNotBeNull();
        category.Name.ShouldBe("Refused");
        category.CountsTowardFailureThresholds.ShouldBeFalse();
        category.IsRetryable.ShouldBeFalse();
    }
}
