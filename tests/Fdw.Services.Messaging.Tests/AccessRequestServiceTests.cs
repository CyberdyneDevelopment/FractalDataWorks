using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// Tests for <see cref="AccessRequestService"/> - access-request workflow logic layered on
/// <see cref="IMessageService"/> (for the associated notification message) and
/// <see cref="IDataGateway"/> (for AccessRequest persistence).
/// </summary>
public sealed class AccessRequestServiceTests
{
    private sealed record Fixture(AccessRequestService Service, Mock<IDataGateway> Gateway, Mock<IMessageService> MessageService);

    private static Fixture CreateService()
    {
        var gateway = new Mock<IDataGateway>(MockBehavior.Loose);
        var messageService = new Mock<IMessageService>(MockBehavior.Loose);
        var service = new AccessRequestService(NullLogger<AccessRequestService>.Instance, gateway.Object, messageService.Object);
        return new Fixture(service, gateway, messageService);
    }

    private static CreateAccessRequest MakeRequest()
        => new()
        {
            TenantId = Guid.NewGuid(),
            RequestingUserId = Guid.NewGuid(),
            RequestedResource = "connections:write",
            RequestedPermission = "write",
            Justification = "need it",
        };

    private static MessagePayload MakeMessageDto(Guid id, Guid? senderUserId = null)
        => new() { Id = id, SenderUserId = senderUserId, MessageType = "AccessRequest", Subject = "s", Status = "New" };

    private static void SetupExecuteInt(Mock<IDataGateway> gateway, IGenericResult<int> result)
        => gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    // ── RequestAccess ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task RequestAccessWithValidRequestCreatesMessageAndAccessRequest()
    {
        var fixture = CreateService();
        var messageId = Guid.NewGuid();
        fixture.MessageService
            .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<MessagePayload>.Success(MakeMessageDto(messageId)));
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var result = await fixture.Service.RequestAccess(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.MessageId.ShouldBe(messageId);
        result.Value!.Status.ShouldBe("Pending");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RequestAccessWhenMessageServiceFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        fixture.MessageService
            .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<MessagePayload>.Failure(new GenericMessage("message failed")));

        var result = await fixture.Service.RequestAccess(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("message failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RequestAccessWhenMessageServiceFailsWithoutMessagesReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.MessageService
            .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<MessagePayload>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.RequestAccess(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RequestAccessWhenInsertFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        fixture.MessageService
            .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<MessagePayload>.Success(MakeMessageDto(Guid.NewGuid())));
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("insert failed")));

        var result = await fixture.Service.RequestAccess(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("insert failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RequestAccessWhenInsertFailsWithoutMessagesReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.MessageService
            .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<MessagePayload>.Success(MakeMessageDto(Guid.NewGuid())));
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.RequestAccess(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RequestAccessWhenMessageServiceThrowsReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.MessageService
            .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.RequestAccess(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RequestAccessWhenDataGatewayThrowsReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.MessageService
            .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<MessagePayload>.Success(MakeMessageDto(Guid.NewGuid())));
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.RequestAccess(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    // ── Approve ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ApproveWithExistingRequestReturnsSuccess()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var result = await fixture.Service.Approve(Guid.NewGuid(), Guid.NewGuid(), "ok", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ApproveWithZeroRowsAffectedReturnsNotFoundFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(0));

        var result = await fixture.Service.Approve(Guid.NewGuid(), Guid.NewGuid(), "ok", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ApproveWhenUpdateFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.Approve(Guid.NewGuid(), Guid.NewGuid(), "ok", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ApproveWhenUpdateFailsWithoutMessagesReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.Approve(Guid.NewGuid(), Guid.NewGuid(), "ok", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ApproveWhenGatewayThrowsReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.Approve(Guid.NewGuid(), Guid.NewGuid(), "ok", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    // ── Deny ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task DenyWithExistingRequestReturnsSuccess()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var result = await fixture.Service.Deny(Guid.NewGuid(), Guid.NewGuid(), "no", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DenyWithZeroRowsAffectedReturnsNotFoundFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(0));

        var result = await fixture.Service.Deny(Guid.NewGuid(), Guid.NewGuid(), "no", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task DenyWhenUpdateFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.Deny(Guid.NewGuid(), Guid.NewGuid(), "no", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task DenyWhenUpdateFailsWithoutMessagesReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.Deny(Guid.NewGuid(), Guid.NewGuid(), "no", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task DenyWhenGatewayThrowsReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.Deny(Guid.NewGuid(), Guid.NewGuid(), "no", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    // ── GetPending ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetPendingReturnsAllPendingRequestsRegardlessOfTenantIdParameter()
    {
        // Why: GetPending's tenantId parameter is never applied to the query - documents a real
        // defect (dead filter parameter); see defectsFound.
        var fixture = CreateService();
        var pending = new[] { new AccessRequestPayload { Id = Guid.NewGuid(), Status = "Pending" } };
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AccessRequestPayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<AccessRequestPayload>>.Success(pending));

        var withTenant = await fixture.Service.GetPending(Guid.NewGuid(), TestContext.Current.CancellationToken);
        var withoutTenant = await fixture.Service.GetPending(null, TestContext.Current.CancellationToken);

        withTenant.IsSuccess.ShouldBeTrue();
        withTenant.Value!.Count.ShouldBe(1);
        withoutTenant.IsSuccess.ShouldBeTrue();
        withoutTenant.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPendingWhenQueryFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AccessRequestPayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<AccessRequestPayload>>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.GetPending(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPendingWhenQueryFailsWithoutMessagesReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AccessRequestPayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<AccessRequestPayload>>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.GetPending(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPendingWhenValueIsNullReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AccessRequestPayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<AccessRequestPayload>>.Success(null!));

        var result = await fixture.Service.GetPending(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPendingWhenGatewayThrowsReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AccessRequestPayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.GetPending(null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }

    // ── GetForUser ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetForUserWithNoMessagesReturnsEmptyList()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<MessagePayload>>.Success(Array.Empty<MessagePayload>()));

        var result = await fixture.Service.GetForUser(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetForUserAggregatesAccessRequestsAcrossMessages()
    {
        var fixture = CreateService();
        var userId = Guid.NewGuid();
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<MessagePayload>>.Success(
            [
                MakeMessageDto(messageId1, userId),
                MakeMessageDto(messageId2, userId)
            ]));
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AccessRequestPayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand cmd, DataStoreTarget target, CancellationToken ct) =>
                GenericResult<IEnumerable<AccessRequestPayload>>.Success([new AccessRequestPayload { Id = Guid.NewGuid() }]));

        var result = await fixture.Service.GetForUser(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetForUserWhenMessageQueryFailsPropagatesFailure()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<MessagePayload>>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.GetForUser(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetForUserWhenAccessRequestQueryFailsPropagatesFailure()
    {
        var fixture = CreateService();
        var userId = Guid.NewGuid();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<MessagePayload>>.Success([MakeMessageDto(Guid.NewGuid(), userId)]));
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AccessRequestPayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<AccessRequestPayload>>.Failure(new GenericMessage("ar boom")));

        var result = await fixture.Service.GetForUser(userId, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("ar boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetForUserWhenGatewayThrowsReturnsAccessRequestFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.GetForUser(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91000");
    }
}
