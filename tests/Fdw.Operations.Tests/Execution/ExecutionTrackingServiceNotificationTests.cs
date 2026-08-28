using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Operations.Data;
using Fdw.Operations.Execution;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// Tests for the notification emission seam in ExecutionTrackingService.Complete().
/// </summary>
public sealed class ExecutionTrackingServiceNotificationTests
{
    private readonly Mock<IDataGateway> _mockGateway;
    private readonly Mock<INotificationServiceProvider> _mockNotificationProvider;
    private readonly Mock<IServiceConfigurationProvider<NotificationRuleConfiguration>> _mockRuleProvider;
    private readonly Mock<INotificationService> _mockNotificationSvc;

    private static readonly NullLoggerFactory LoggerFactory = NullLoggerFactory.Instance;

    public ExecutionTrackingServiceNotificationTests()
    {
        _mockGateway = new Mock<IDataGateway>();
        _mockNotificationProvider = new Mock<INotificationServiceProvider>();
        _mockRuleProvider = new Mock<IServiceConfigurationProvider<NotificationRuleConfiguration>>();
        _mockNotificationSvc = new Mock<INotificationService>();
    }

    // ==========================================================================
    // Helper: build a running ExecutionItem root (ParentExecutionItemId = null)
    // ==========================================================================

    private static ExecutionItem BuildRunningRootItem(Guid id)
    {
        var item = new ExecutionItem
        {
            Id = id,
            ParentExecutionItemId = null,
            RootExecutionItemId = id,
            ItemType = "Workflow",
            Name = "Test Workflow",
            State = "Running",
            CorrelationId = "corr-123",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        return item;
    }

    // ==========================================================================
    // Helper: configure DataGateway to handle all calls made by Complete()
    // ==========================================================================

    private void SetupGatewayForComplete(ExecutionItem item)
    {
        // GetItemInternal (called twice: once in Complete, once in TransitionState)
        _mockGateway
            .Setup(g => g.Execute<IEnumerable<ExecutionItem>>(
                It.IsAny<IDataCommand>(),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExecutionItem>>.Success(new[] { item }));

        // PersistItemUpdate and TransitionState's PersistItemUpdate (int results)
        _mockGateway
            .Setup(g => g.Execute<int>(
                It.IsAny<IDataCommand>(),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        // GetNextSequenceNumber and RecordEventInternal use ExecutionEvent queries
        _mockGateway
            .Setup(g => g.Execute<IEnumerable<ExecutionEvent>>(
                It.IsAny<IDataCommand>(),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExecutionEvent>>.Success(Array.Empty<ExecutionEvent>()));
    }

    // ==========================================================================
    // Helper: build a catch-all enabled rule
    // ==========================================================================

    private static NotificationRuleConfiguration BuildCatchAllRule(string name, string serviceName, string severity = "Info")
    {
        return new NotificationRuleConfiguration
        {
            Name = name,
            IsEnabled = true,
            NotificationServiceName = serviceName,
            NotificationServiceType = "Console",
            Severity = severity,
            PipelineId = null,
            WorkflowId = null,
            ScheduleId = null
        };
    }

    // ==========================================================================
    // Test 1: Root terminal execution + catch-all rule with default "Info" severity → Send called once
    //         Proves that the default severity emits (Info maps to Normal priority).
    // ==========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Notifications")]
    public async Task CompleteRootItemWithMatchingRule_CallsSendOnce()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = BuildRunningRootItem(itemId);
        SetupGatewayForComplete(item);

        var rule = BuildCatchAllRule("CatchAll", "console-channel");
        _mockRuleProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<NotificationRuleConfiguration>>.Success(
                new List<NotificationRuleConfiguration> { rule }));

        _mockNotificationProvider
            .Setup(p => p.Get("console-channel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPlatformNotification>.Success(_mockNotificationSvc.Object));

        _mockNotificationSvc
            .Setup(s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationResult>.Success(
                new Mock<INotificationResult>().Object));

        var sut = new ExecutionTrackingService(
            _mockGateway.Object,
            LoggerFactory,
            "OpsDb",
            _mockNotificationProvider.Object,
            _mockRuleProvider.Object);

        // Act
        var result = await sut.Complete(itemId, success: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockNotificationProvider.Verify(
            p => p.Get("console-channel", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockNotificationSvc.Verify(
            s => s.Send(
                It.Is<INotificationRequest>(r => r.ChannelName == "Console"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ==========================================================================
    // Test 2: Non-root execution (ParentId set) → Send NOT called
    // ==========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Notifications")]
    public async Task CompleteChildItem_DoesNotCallSend()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var domainConfigurationId = Guid.NewGuid();
        var item = BuildRunningRootItem(itemId);
        item.ParentExecutionItemId = domainConfigurationId; // make it a child

        SetupGatewayForComplete(item);

        var rule = BuildCatchAllRule("CatchAll", "console-channel");
        _mockRuleProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<NotificationRuleConfiguration>>.Success(
                new List<NotificationRuleConfiguration> { rule }));

        var sut = new ExecutionTrackingService(
            _mockGateway.Object,
            LoggerFactory,
            "OpsDb",
            _mockNotificationProvider.Object,
            _mockRuleProvider.Object);

        // Act
        var result = await sut.Complete(itemId, success: false, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockNotificationSvc.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ==========================================================================
    // Test 3: No matching rule (disabled rule) → Send NOT called
    // ==========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Notifications")]
    public async Task CompleteRootItemWithDisabledRule_DoesNotCallSend()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = BuildRunningRootItem(itemId);
        SetupGatewayForComplete(item);

        var disabledRule = new NotificationRuleConfiguration
        {
            Name = "DisabledRule",
            IsEnabled = false,
            NotificationServiceName = "console-channel",
            NotificationServiceType = "Console",
            Severity = "Normal"
        };
        _mockRuleProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<NotificationRuleConfiguration>>.Success(
                new List<NotificationRuleConfiguration> { disabledRule }));

        var sut = new ExecutionTrackingService(
            _mockGateway.Object,
            LoggerFactory,
            "OpsDb",
            _mockNotificationProvider.Object,
            _mockRuleProvider.Object);

        // Act
        var result = await sut.Complete(itemId, success: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockNotificationSvc.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ==========================================================================
    // Test 3b: No matching rule (scope mismatch) → Send NOT called
    // ==========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Notifications")]
    public async Task CompleteRootItemWithScopeMismatchRule_DoesNotCallSend()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = BuildRunningRootItem(itemId);
        SetupGatewayForComplete(item);

        // Rule scoped to a specific workflow that doesn't match this execution
        var scopedRule = new NotificationRuleConfiguration
        {
            Name = "ScopedRule",
            IsEnabled = true,
            NotificationServiceName = "console-channel",
            NotificationServiceType = "Console",
            Severity = "Normal",
            WorkflowId = Guid.NewGuid() // different workflow
        };
        _mockRuleProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<NotificationRuleConfiguration>>.Success(
                new List<NotificationRuleConfiguration> { scopedRule }));

        var sut = new ExecutionTrackingService(
            _mockGateway.Object,
            LoggerFactory,
            "OpsDb",
            _mockNotificationProvider.Object,
            _mockRuleProvider.Object);

        // Act
        var result = await sut.Complete(itemId, success: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockNotificationSvc.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ==========================================================================
    // Test 4: notificationProvider null (not wired) → no throw, Send not called, Complete succeeds
    // ==========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Notifications")]
    public async Task CompleteWithNullNotificationProvider_Succeeds_WithoutCallingRuleProvider()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = BuildRunningRootItem(itemId);
        SetupGatewayForComplete(item);

        var sut = new ExecutionTrackingService(
            _mockGateway.Object,
            LoggerFactory,
            "OpsDb",
            notificationProvider: null,
            notificationRuleProvider: null);

        // Act
        var result = await sut.Complete(itemId, success: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockRuleProvider.Verify(
            p => p.Get(It.IsAny<CancellationToken>()),
            Times.Never);
        _mockNotificationSvc.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ==========================================================================
    // Test 5a: Unrecognised severity → Send NOT called, invalid-severity path taken
    // ==========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Notifications")]
    public async Task CompleteRootItemWithUnrecognisedSeverity_DoesNotCallSend()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = BuildRunningRootItem(itemId);
        SetupGatewayForComplete(item);

        var rule = BuildCatchAllRule("BogusRule", "console-channel", severity: "Bogus");
        _mockRuleProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<NotificationRuleConfiguration>>.Success(
                new List<NotificationRuleConfiguration> { rule }));

        _mockNotificationProvider
            .Setup(p => p.Get("console-channel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPlatformNotification>.Success(_mockNotificationSvc.Object));

        var sut = new ExecutionTrackingService(
            _mockGateway.Object,
            LoggerFactory,
            "OpsDb",
            _mockNotificationProvider.Object,
            _mockRuleProvider.Object);

        // Act
        var result = await sut.Complete(itemId, success: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert — Complete succeeds but Send was never called because the rule was skipped
        result.IsSuccess.ShouldBeTrue();
        _mockNotificationSvc.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ==========================================================================
    // Test 5: Send failure → Complete still returns success (emission is auxiliary)
    // ==========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Notifications")]
    public async Task CompleteSendFailure_DoesNotPropagateToCompleteResult()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = BuildRunningRootItem(itemId);
        SetupGatewayForComplete(item);

        var rule = BuildCatchAllRule("CatchAll", "console-channel");
        _mockRuleProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<NotificationRuleConfiguration>>.Success(
                new List<NotificationRuleConfiguration> { rule }));

        _mockNotificationProvider
            .Setup(p => p.Get("console-channel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPlatformNotification>.Success(_mockNotificationSvc.Object));

        _mockNotificationSvc
            .Setup(s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationResult>.Failure(new GenericMessage("Send failed")));

        var sut = new ExecutionTrackingService(
            _mockGateway.Object,
            LoggerFactory,
            "OpsDb",
            _mockNotificationProvider.Object,
            _mockRuleProvider.Object);

        // Act
        var result = await sut.Complete(itemId, success: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert — execution result unaffected by emission failure
        result.IsSuccess.ShouldBeTrue();
    }
}
