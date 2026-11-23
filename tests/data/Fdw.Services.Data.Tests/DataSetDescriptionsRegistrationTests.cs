using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A host that registered the data set domain without the Descriptions service type is refused at Initialize with a coded message (FDW-6121), where it used to get a provider
/// that failed with a null on its first save.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DataSetDescriptionsRegistrationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void TheMissingDescriptionWriterHasItsOwnCodedMessageThatNamesTheServiceTypeToRegister()
    {
        var message = DataSetDescriptionsRegistrationLog.DescriptionWriterNotRegistered(NullLogger.Instance);

        message.Code.ShouldBe("DATA-60342");
        message.Message.ShouldContain("IDescriptionWriter");
        message.Message.ShouldContain("DescriptionServiceTypes");
        message.Message.ShouldContain("data store");
    }
}
