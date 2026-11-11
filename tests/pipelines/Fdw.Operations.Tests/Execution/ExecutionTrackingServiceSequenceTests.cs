using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Data;
using Fdw.Operations.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// FDW-4945: when the last event sequence number of an item cannot be read, no event is written with a guessed number; the
/// transition or event fails with its own code.
/// </summary>
public sealed class ExecutionTrackingServiceSequenceTests
{
    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private readonly Mock<IDataGateway> _gateway = new();
    private readonly Guid _itemId = Guid.NewGuid();

    private ExecutionTrackingService Service(bool sequenceReadFails)
    {
        var item = new ExecutionItem
        {
            Id = _itemId,
            RootExecutionItemId = _itemId,
            ItemType = "Workflow",
            Name = "Test Workflow",
            State = "Running",
            CorrelationId = "corr",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        _gateway
            .Setup(g => g.Execute<IEnumerable<ExecutionItem>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExecutionItem>>.Success(new[] { item }));
        _gateway
            .Setup(g => g.Execute<IEnumerable<ExecutionEvent>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequenceReadFails
                ? GenericResult<IEnumerable<ExecutionEvent>>.Failure(new GenericMessage("the event read failed"))
                : GenericResult<IEnumerable<ExecutionEvent>>.Success(Array.Empty<ExecutionEvent>()));
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(new SystemAuthenticationContext());
        return new ExecutionTrackingService(new StubGatewayProvider(_gateway.Object), NullLoggerFactory.Instance, accessor.Object, "OpsDb");
    }

    private void NothingWasWritten()
        => _gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnEventWhoseSequenceCannotBeReadFailsWithItsOwnCodeAndWritesNothing()
    {
        // Act
        var result = await Service(sequenceReadFails: true).RecordEvent(_itemId, "Note", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("OPERATIONS-71005");
        result.Messages.Select(m => m.Message).ShouldContain("the event read failed");
        NothingWasWritten();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATransitionWhoseSequenceCannotBeReadFailsBeforeItChangesTheItem()
    {
        // Act
        var result = await Service(sequenceReadFails: true).TransitionState(
            _itemId, ExecutionStateTypes.ByName("Completed"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("OPERATIONS-71005");
        NothingWasWritten();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheFirstEventOfAnItemWithNoneIsNumberedOne()
    {
        // Act
        var result = await Service(sequenceReadFails: false).RecordEvent(_itemId, "Note", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.SequenceNumber.ShouldBe(1);
    }
}
