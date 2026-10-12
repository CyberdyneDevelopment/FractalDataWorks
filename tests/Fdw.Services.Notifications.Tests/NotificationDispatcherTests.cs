using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Notifications.Tests;

/// <summary>
/// Tests for <see cref="NotificationDispatcher"/> covering channel routing, validation
/// short-circuiting, result propagation, batch aggregation, and channel introspection.
/// </summary>
public sealed class NotificationDispatcherTests
{
    private static Mock<INotificationService> CreateServiceMock(string channelName)
    {
        var mock = new Mock<INotificationService>();
        mock.Setup(s => s.Channel).Returns(NotificationChannels.ByName(channelName));
        return mock;
    }

    private static NotificationDispatcher CreateDispatcher(params INotificationService[] services)
        => new(services, NullLogger<NotificationDispatcher>.Instance);

    private static NotificationRequest CreateRequest(
        string channelName,
        string message = "hello",
        IEnumerable<string>? recipients = null)
    {
        var builder = NotificationRequest.Create(channelName)
            .WithSubject("subject")
            .WithMessage(message)
            .To(recipients ?? new[] { "user@example.com" });
        return builder.Build();
    }

    // ───────────────────────────── Constructor / channel lookup ─────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsChannelAvailableReturnsTrueForRegisteredChannelRegardlessOfCase()
    {
        // Arrange
        var dispatcher = CreateDispatcher(CreateServiceMock("Email").Object);

        // Act & Assert
        dispatcher.IsChannelAvailable("Email").ShouldBeTrue();
        dispatcher.IsChannelAvailable("email").ShouldBeTrue();
        dispatcher.IsChannelAvailable("EMAIL").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsChannelAvailableReturnsFalseWhenNoServiceRegisteredForChannel()
    {
        // Arrange
        var dispatcher = CreateDispatcher(CreateServiceMock("Email").Object);

        // Act
        var result = dispatcher.IsChannelAvailable("Webhook");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void GetAvailableChannelsReturnsOneChannelPerRegisteredService()
    {
        // Arrange
        var dispatcher = CreateDispatcher(
            CreateServiceMock("Email").Object,
            CreateServiceMock("Webhook").Object);

        // Act
        var channels = dispatcher.GetAvailableChannels().ToList();

        // Assert
        channels.Count.ShouldBe(2);
        channels.Select(c => c.Name).ShouldContain("Email");
        channels.Select(c => c.Name).ShouldContain("Webhook");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void GetAvailableChannelsReturnsEmptyWhenNoServicesRegistered()
    {
        // Arrange
        var dispatcher = CreateDispatcher();

        // Act
        var channels = dispatcher.GetAvailableChannels();

        // Assert
        channels.ShouldBeEmpty();
    }

    // ───────────────────────────────────────── Send ─────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendReturnsChannelNotFoundWhenNoServiceIsRegisteredForTheRequestedChannel()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = CreateRequest("Unregistered");

        // Act
        var result = await dispatcher.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ChannelNotFound");
        result.Details.ShouldNotBeNull();
        result.Details!.GetValue<string>("ChannelName").ShouldBe("Unregistered");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendReturnsValidationFailedWhenServiceValidationFailsAndNeverCallsSend()
    {
        // Arrange
        var service = CreateServiceMock("Email");
        service.Setup(s => s.Validate(It.IsAny<INotificationRequest>()))
            .Returns(GenericResult.Failure(NotificationResultCodes.ByName("EmptyMessage")));
        var dispatcher = CreateDispatcher(service.Object);
        var request = CreateRequest("Email");

        // Act
        var result = await dispatcher.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ValidationFailed");
        service.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendReturnsTheServiceResultUnchangedWhenValidationAndSendSucceed()
    {
        // Arrange
        var service = CreateServiceMock("Email");
        service.Setup(s => s.Validate(It.IsAny<INotificationRequest>())).Returns(GenericResult.Success());
        var expected = GenericResult<INotificationResult>.Success(NotificationResult.Success("req-1"));
        service.Setup(s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var dispatcher = CreateDispatcher(service.Object);
        var request = CreateRequest("Email");

        // Act
        var result = await dispatcher.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeSameAs(expected);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendReturnsTheServiceResultUnchangedWhenSendFails()
    {
        // Arrange
        var service = CreateServiceMock("Email");
        service.Setup(s => s.Validate(It.IsAny<INotificationRequest>())).Returns(GenericResult.Success());
        var expected = GenericResult<INotificationResult>.Failure(NotificationResultCodes.ByName("SendFailed"));
        service.Setup(s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var dispatcher = CreateDispatcher(service.Object);
        var request = CreateRequest("Email");

        // Act
        var result = await dispatcher.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeSameAs(expected);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendRoutesToTheServiceMatchingTheRequestChannelNameOnly()
    {
        // Arrange
        var email = CreateServiceMock("Email");
        email.Setup(s => s.Validate(It.IsAny<INotificationRequest>())).Returns(GenericResult.Success());
        var emailResult = GenericResult<INotificationResult>.Success(NotificationResult.Success("e1"));
        email.Setup(s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResult);
        var webhook = CreateServiceMock("Webhook");
        var dispatcher = CreateDispatcher(email.Object, webhook.Object);
        var request = CreateRequest("Email");

        // Act
        var result = await dispatcher.Send(request, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeSameAs(emailResult);
        webhook.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        webhook.Verify(
            s => s.Validate(It.IsAny<INotificationRequest>()),
            Times.Never);
    }

    // ─────────────────────────────────────── SendBatch ──────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SendBatchReturnsEmptySuccessForAnEmptyRequestList()
    {
        // Arrange
        var dispatcher = CreateDispatcher();

        // Act
        var result = await dispatcher.SendBatch(Array.Empty<INotificationRequest>(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendBatchAggregatesSuccessAndFailureCountsAcrossRequests()
    {
        // Arrange
        var service = CreateServiceMock("Email");
        service.Setup(s => s.Validate(It.IsAny<INotificationRequest>())).Returns(GenericResult.Success());
        service.SetupSequence(s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationResult>.Success(NotificationResult.Success("1")))
            .ReturnsAsync(GenericResult<INotificationResult>.Success(NotificationResult.Failed("2", "boom")));
        var dispatcher = CreateDispatcher(service.Object);
        var requests = new[] { CreateRequest("Email"), CreateRequest("Email") };

        // Act
        var result = await dispatcher.SendBatch(requests, TestContext.Current.CancellationToken);

        // Assert — SendBatch itself always reports success; per-item outcomes ride in the payload.
        result.IsSuccess.ShouldBeTrue();
        var items = result.Value.ShouldNotBeNull().ToList();
        items.Count.ShouldBe(2);
        items.Count(r => r.IsSuccess).ShouldBe(1);
        items.Count(r => !r.IsSuccess).ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SendBatchAddsAFailedPlaceholderWhenTheOuterSendResultIsUnsuccessful()
    {
        // Arrange — no service registered, so Send() itself returns a non-success outer result.
        var dispatcher = CreateDispatcher();
        var request = CreateRequest("Unregistered");

        // Act
        var result = await dispatcher.SendBatch(new[] { request }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var item = result.Value.ShouldNotBeNull().Single();
        item.IsSuccess.ShouldBeFalse();
        item.RequestId.ShouldBe(request.RequestId);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SendBatchStopsBeforeProcessingAnyRequestWhenCancellationIsAlreadyRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = CreateServiceMock("Email");
        var dispatcher = CreateDispatcher(service.Object);
        var requests = new[] { CreateRequest("Email") };

        // Act
        var result = await dispatcher.SendBatch(requests, cts.Token);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
        service.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task SendBatchStopsMidwayWhenCancellationIsRequestedBetweenRequests()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var service = CreateServiceMock("Email");
        service.Setup(s => s.Validate(It.IsAny<INotificationRequest>())).Returns(GenericResult.Success());
        service.Setup(s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationResult>.Success(NotificationResult.Success("1")))
            .Callback(() => cts.Cancel());
        var dispatcher = CreateDispatcher(service.Object);
        var requests = new[] { CreateRequest("Email"), CreateRequest("Email"), CreateRequest("Email") };

        // Act
        var result = await dispatcher.SendBatch(requests, cts.Token);

        // Assert — only the first request is processed; the loop breaks before the second.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Count().ShouldBe(1);
        service.Verify(
            s => s.Send(It.IsAny<INotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
