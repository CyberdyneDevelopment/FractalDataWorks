using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Execution;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

using Fdw.Services.Data;
namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// Proves the tracker is constructible without an operational store and reports the absence per call.
/// </summary>
/// <remarks>
/// This is the regression guard for a live outage. The store name was once proven in the Operations
/// Registration phase, which took every reference host down at boot: every host registers the
/// Operations domain as part of the platform sweep, including hosts that never track an execution and
/// therefore have no operational store to name. Construction is not use, so the check belongs at the
/// reads. That is only workable because every public method here returns a result and can say no.
/// </remarks>
public sealed class ExecutionTrackingServiceUnnamedStoreTests
{
    // A stub rather than the real provider: this fixture is about what the service does with a
    // gateway, not about how one is supplied.
    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private static ExecutionTrackingService WithNoStore() =>
        new(new StubGatewayProvider(new Mock<IDataGateway>(MockBehavior.Strict).Object), NullLoggerFactory.Instance, dataStoreName: null);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConstructingWithoutAStoreDoesNotThrow()
    {
        // A host that never tracks an execution must still be able to build the platform.
        Should.NotThrow(() => WithNoStore());
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData("GetItem")]
    [InlineData("GetEvents")]
    [InlineData("GetChildren")]
    [InlineData("GetItems")]
    public async Task ReadsFailLoudRatherThanQueryingAStoreNobodyNamed(string operation)
    {
        var tracker = WithNoStore();
        var ct = TestContext.Current.CancellationToken;

        // The gateway is Strict: if any of these reached it, the mock would throw instead of failing.
        var message = operation switch
        {
            "GetItem" => (await tracker.GetItem(Guid.NewGuid(), ct)).CurrentMessage?.ToString(),
            "GetEvents" => (await tracker.GetEvents(Guid.NewGuid(), ct)).CurrentMessage?.ToString(),
            "GetChildren" => (await tracker.GetChildren(Guid.NewGuid(), ct)).CurrentMessage?.ToString(),
            "GetItems" => (await tracker.GetItems("correlation-id", ct)).CurrentMessage?.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

        message.ShouldNotBeNull();
        message.ShouldContain("OperationalConnection");
    }
}
