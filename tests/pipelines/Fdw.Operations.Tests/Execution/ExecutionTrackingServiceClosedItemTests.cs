using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Operations.Data;
using Fdw.Operations.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// FDW-6986: an execution item that has ended is not written again. A run whose record the recovery of half-done runs closed Interrupted may reach its own end afterwards; that second end must
/// neither reopen the item nor overwrite the code and the message of the first, or the item would say one thing in its state and another in its reason.
/// </summary>
public sealed class ExecutionTrackingServiceClosedItemTests
{
    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private static IAuthenticationContextAccessor SystemContext()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(new SystemAuthenticationContext());
        return accessor.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASecondEndOfAnItemThatEndedWritesNothingAndLeavesTheFirstEndsStateCodeAndMessage()
    {
        var id = Guid.NewGuid();
        var item = new ExecutionItem
        {
            Id = id, RootExecutionItemId = id, ItemType = "Workflow", Name = "Turn", State = "Interrupted", ResultCode = "PipelineRunEndedHalfDone", ResultMessage = "ended half-done",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1), CompletedAt = DateTimeOffset.UtcNow,
        };
        var gateway = new Mock<IDataGateway>(MockBehavior.Strict);
        gateway.Setup(g => g.Execute<IEnumerable<ExecutionItem>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExecutionItem>>.Success(new[] { item }));
        var sut = new ExecutionTrackingService(new StubGatewayProvider(gateway.Object), NullLoggerFactory.Instance, SystemContext(), "OpsDb", null, null);

        var second = await sut.Complete(id, success: false, resultCode: "Failed", resultMessage: "the claim lapsed", cancellationToken: TestContext.Current.CancellationToken);

        second.IsFailure.ShouldBeTrue();
        item.State.ShouldBe("Interrupted");
        item.ResultCode.ShouldBe("PipelineRunEndedHalfDone");
        item.ResultMessage.ShouldBe("ended half-done");
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
