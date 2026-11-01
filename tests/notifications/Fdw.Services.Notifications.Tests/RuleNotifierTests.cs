using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Notifications.Tests;

/// <summary>
/// Tests for <see cref="RuleNotifier"/> (FDW-1120): a notification goes through its named rule, the rule's own conditions
/// decide whether it sends, and a rule, channel or severity that cannot be used is a loud failure rather than a skipped send.
/// </summary>
public sealed class RuleNotifierTests
{
    private readonly Mock<INotificationRuleConfigurationProvider> _rules = new();
    private readonly Mock<INotificationServiceProvider> _services = new();
    private readonly Mock<INotificationService> _channel = new();
    private readonly List<INotificationRequest> _sent = [];

    private RuleNotifier Sut() => new(_rules.Object, _services.Object, NullLogger<RuleNotifier>.Instance);

    private static NotificationRuleImplementationConfiguration Rule(string severity = "Info", bool enabled = true, Guid? pipeline = null) => new()
    {
        Name = "ops-alerts",
        IsEnabled = enabled,
        NotificationServiceName = "console-channel",
        NotificationServiceType = "Console",
        Severity = severity,
        PipelineId = pipeline
    };

    private static RuleNotification Notification(Guid? pipelineId = null) => new()
    {
        RuleName = "ops-alerts",
        Subject = "copy: Alert",
        Message = "113 rows loaded",
        PipelineId = pipelineId,
        Metadata = new Dictionary<string, object?> { ["RowsLoaded"] = "113" },
        CorrelationId = "run-1"
    };

    private void RuleExists(NotificationRuleImplementationConfiguration rule) =>
        _rules.Setup(r => r.Get("ops-alerts", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationRuleImplementationConfiguration>.Success(rule));

    private void RuleMissing() =>
        _rules.Setup(r => r.Get("ops-alerts", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationRuleImplementationConfiguration>.Failure(new GenericMessage("no such rule")));

    private void ChannelResolves(bool sends = true)
    {
        _services.Setup(s => s.Get("console-channel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPlatformNotification>.Success(_channel.Object));
        _channel.Setup(c => c.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<INotificationRequest, CancellationToken>((request, _) => _sent.Add(request))
            .ReturnsAsync(sends
                ? GenericResult<INotificationResult>.Success(new Mock<INotificationResult>().Object)
                : GenericResult<INotificationResult>.Failure(new GenericMessage("smtp refused")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public async Task NotifySendsThroughTheRulesChannelCarryingTheCallersWordsAndMetadata()
    {
        // Arrange
        RuleExists(Rule());
        ChannelResolves();

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var request = _sent.ShouldHaveSingleItem();
        request.ChannelName.ShouldBe("Console");
        request.Subject.ShouldBe("copy: Alert");
        request.Message.ShouldBe("113 rows loaded");
        request.CorrelationId.ShouldBe("run-1");
        request.Metadata!["RowsLoaded"].ShouldBe("113");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    [InlineData("Low", "Low")]
    [InlineData("Info", "Normal")]
    [InlineData("information", "Normal")]
    [InlineData("Warning", "High")]
    [InlineData("Error", "Critical")]
    [InlineData("Fatal", "Critical")]
    public async Task TheRulesSeverityBecomesTheDeliveryPriority(string severity, string priority)
    {
        // Arrange
        RuleExists(Rule(severity));
        ChannelResolves();

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _sent.ShouldHaveSingleItem().Priority.Name.ShouldBe(priority);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public async Task ADisabledRuleDeclinesAndSendsNothing()
    {
        // Arrange
        RuleExists(Rule(enabled: false));
        ChannelResolves();

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert: declining is a success that sent nothing
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public async Task ARuleScopedToAnotherPipelineDeclinesAndARuleScopedToThisOneSends()
    {
        // Arrange
        var mine = Guid.NewGuid();
        ChannelResolves();

        // Act + Assert: another pipeline's rule declines
        RuleExists(Rule(pipeline: Guid.NewGuid()));
        (await Sut().Notify(Notification(mine), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        _sent.ShouldBeEmpty();

        // Act + Assert: this pipeline's rule sends
        RuleExists(Rule(pipeline: mine));
        (await Sut().Notify(Notification(mine), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        _sent.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARuleThatDoesNotExistFailsWithRuleNotFoundAndNothingIsSent()
    {
        // Arrange
        RuleMissing();
        ChannelResolves();

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBe(NotificationResultCodes.ByName("RuleNotFound"));
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("ops-alerts");
        _sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRuleNotFoundMessageNamesTheRuleAndLeavesNoEmptyParentheses()
    {
        // Arrange
        RuleMissing();
        ChannelResolves();

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("'ops-alerts'");
        message.ShouldNotContain("()");
        message.TrimEnd().ShouldEndWith("was not found");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASeverityThatMapsToNoPriorityFailsWithRuleSeverityInvalidAndNothingIsSent()
    {
        // Arrange
        RuleExists(Rule("Dramatic"));
        ChannelResolves();

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBe(NotificationResultCodes.ByName("RuleSeverityInvalid"));
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Dramatic");
        _sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChannelThatCannotBeResolvedFailsWithChannelNotFound()
    {
        // Arrange
        RuleExists(Rule());
        _services.Setup(s => s.Get("console-channel", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPlatformNotification>.Failure(new GenericMessage("no such channel")));

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBe(NotificationResultCodes.ByName("ChannelNotFound"));
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("console-channel");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChannelThatRefusesTheSendFailsWithSendFailedAndSaysWhy()
    {
        // Arrange
        RuleExists(Rule());
        ChannelResolves(sends: false);

        // Act
        var result = await Sut().Notify(Notification(), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBe(NotificationResultCodes.ByName("SendFailed"));
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("smtp refused");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Notifications")]
    public async Task ExistsSucceedsForAConfiguredRuleAndFailsWithRuleNotFoundForAMissingOne()
    {
        // Arrange + Act
        RuleExists(Rule());
        var found = await Sut().Exists("ops-alerts", TestContext.Current.CancellationToken);
        RuleMissing();
        var missing = await Sut().Exists("ops-alerts", TestContext.Current.CancellationToken);

        // Assert
        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        missing.IsFailure.ShouldBeTrue();
        missing.Code.ShouldBe(NotificationResultCodes.ByName("RuleNotFound"));
    }
}
