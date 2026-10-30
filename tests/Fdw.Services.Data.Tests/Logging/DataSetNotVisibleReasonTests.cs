using Fdw.Services.Data.Logging;

namespace Fdw.Services.Data.Tests.Logging;

/// <summary>
/// Tests for the reason a data set lookup gives when the provider answers success with no data set (FDW-3170): it names the tenant cause, since a
/// data set that another tenant owns is hidden from the caller and cannot be told from one that does not exist.
/// </summary>
public sealed class DataSetNotVisibleReasonTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Data")]
    public void TheReasonSaysTheDataSetMayExistUnderAnotherTenant()
    {
        DataGatewayLogger.DataSetNotVisibleReason.ShouldContain("tenant");
        DataGatewayLogger.DataSetNotVisibleReason.ShouldContain("does not exist");
        DataGatewayLogger.DataSetNotVisibleReason.ShouldContain("another tenant owns it");
    }
}
