using Fdw.Services.Messaging.Configuration;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Hubs;
using Fdw.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Data;
namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// Tests for <see cref="MessageService"/> - message CRUD/lifecycle dispatch logic backed by
/// <see cref="IDataGateway"/> and SignalR push via <see cref="IHubContext{MessageHub,IMessageHubClient}"/>.
/// Only the gateway and hub context are faked.
/// </summary>
public sealed class MessageServiceTests
{
    // A stub rather than the real provider: this fixture is about what the service does with a
    // gateway, not about how one is supplied.
    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private sealed class StubAccessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private sealed record Fixture(
        MessageService Service,
        Mock<IDataGateway> Gateway,
        Mock<IMessageHubClient> Client,
        List<string> TargetedGroups,
        StubAccessor Accessor,
        List<AlertActionRow> Actions)
    {
        /// <summary>Signs in a person who holds the right to read messages, as the alert questions are asked.</summary>
        public Fixture SignedInAs(Guid userId) => SignedIn(userId, ["messages:read"]);

        /// <summary>Signs in a person who holds no right to read messages.</summary>
        public Fixture SignedInWithoutRights(Guid userId) => SignedIn(userId, []);

        private Fixture SignedIn(Guid userId, string[] permissions)
        {
            var context = new Mock<IAuthenticationContext>(MockBehavior.Loose);
            context.SetupGet(c => c.IsAuthenticated).Returns(true);
            context.SetupGet(c => c.UserId).Returns(userId.ToString("D"));
            context.SetupGet(c => c.Permissions).Returns(permissions);
            Accessor.Current = context.Object;
            return this;
        }
    }

    private static Fixture CreateService()
    {
        var gateway = new Mock<IDataGateway>(MockBehavior.Loose);
        var client = new Mock<IMessageHubClient>();
        var groups = new List<string>();
        var clients = new Mock<IHubClients<IMessageHubClient>>();
        clients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string g) => { groups.Add(g); return client.Object; });
        var hubContext = new Mock<IHubContext<MessageHub, IMessageHubClient>>();
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);

        var messaging = new Mock<IMessagingConfigurationProvider>(MockBehavior.Loose);
        messaging
            .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IMessagingImplementationConfiguration>.Success(
                new MessagingImplementationConfiguration { Name = "Messaging", DataStoreName = "OpsDb", PathName = "msg" }));

        // What people did to alerts: a message read asks for the caller's Dismiss rows, and the tests that care seed them here.
        var actions = new List<AlertActionRow>();
        gateway
            .Setup(g => g.Execute<IEnumerable<AlertActionRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IEnumerable<AlertActionRow>>.Success(actions.ToList()));

        var accessor = new StubAccessor();
        var service = new MessageService(
            NullLogger<MessageService>.Instance, new StubGatewayProvider(gateway.Object), messaging.Object, hubContext.Object, accessor);
        return new Fixture(service, gateway, client, groups, accessor, actions);
    }

    private static CreateMessageRequest MakeRequest(Guid? recipientUserId = null)
        => new()
        {
            TenantId = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            SenderUserId = Guid.NewGuid(),
            MessageType = "Info",
            Subject = "subject",
            Body = "body",
        };

    private static MessagePayload MakeDto(Guid id, Guid? recipientUserId)
        => new()
        {
            Id = id,
            TenantId = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            MessageType = "Info",
            Severity = "Info",
            Subject = "subject",
            Status = "New",
            CreatedAt = DateTime.UtcNow,
        };

    private static void SetupExecuteInt(Mock<IDataGateway> gateway, IGenericResult<int> result)
        => gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private static void SetupExecuteMessages(Mock<IDataGateway> gateway, IGenericResult<IEnumerable<MessagePayload>> result)
        => gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    // ── GetMessages ordering pushdown ───────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetMessagesPutsTheOrderingOnTheCommandRatherThanSortingInMemory()
    {
        // Why assert the command and not the returned order: the gateway is a mock returning a fixed
        // list, so an assertion on the result order passes whether the ordering was pushed down or
        // applied in the host afterwards. Only the command carries the difference. SQL translators
        // declare CanExpressOrdering, so an ordering set here becomes ORDER BY in the generated SQL.
        var fixture = CreateService();
        IDataCommand? captured = null;
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand c, DataStoreTarget _, CancellationToken __) => captured = c)
            .ReturnsAsync(GenericResult<IEnumerable<MessagePayload>>.Success([]));

        await fixture.Service.GetMessages(new MessageQuery(), TestContext.Current.CancellationToken);

        var query = captured.ShouldBeAssignableTo<IQueryCommand>()!;
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields.Select(f => f.PropertyName)
            .ShouldBe([nameof(MessagePayload.CreatedAt), nameof(MessagePayload.Id)]);
        query.Ordering.OrderedFields.ShouldAllBe(f => f.Direction.Name == "Ascending");
    }

    // ── CreateMessage ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateMessageWithDirectRecipientInsertsAndNotifiesViaSignalR()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));
        var recipientId = Guid.NewGuid();

        var result = await fixture.Service.CreateMessage(MakeRequest(recipientId), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RecipientUserId.ShouldBe(recipientId);
        fixture.TargetedGroups.ShouldContain(HubScopes.User(recipientId.ToString("D")));
        fixture.Client.Verify(c => c.NewMessage(It.IsAny<MessagePayload>()), Times.Once);
        fixture.Client.Verify(c => c.UnreadCountChanged(), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateMessageWithoutRecipientDoesNotNotifyViaSignalR()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var result = await fixture.Service.CreateMessage(MakeRequest(recipientUserId: null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.TargetedGroups.ShouldBeEmpty();
        fixture.Client.Verify(c => c.NewMessage(It.IsAny<MessagePayload>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateMessageWhenInsertFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("insert exploded")));

        var result = await fixture.Service.CreateMessage(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("insert exploded");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateMessageWhenInsertFailsWithoutMessagesReturnsMessageCreationFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.CreateMessage(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateMessageWhenGatewayThrowsReturnsMessageCreationFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.CreateMessage(MakeRequest(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateMessageWhenRecipientInsertFailsStillReturnsSuccessAndNotifies()
    {
        var fixture = CreateService();
        fixture.Gateway
            .SetupSequence(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("recipient insert exploded")));

        var result = await fixture.Service.CreateMessage(MakeRequest(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Client.Verify(c => c.NewMessage(It.IsAny<MessagePayload>()), Times.Once);
    }

    // ── GetMessages ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetMessagesWithAllFiltersAppliesPagingAndReturnsResults()
    {
        // Why the command is asserted rather than the returned count: paging is the STORE's job now,
        // and the gateway is a mock that returns its fixed list whatever window it is handed. This
        // test previously stubbed three rows and asserted one came back, which only held while the
        // host sliced the results — it would pass again the moment paging regressed to in-memory.
        var fixture = CreateService();
        var userId = Guid.NewGuid();
        IDataCommand? captured = null;
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand c, DataStoreTarget _, CancellationToken __) => captured = c)
            .ReturnsAsync(GenericResult<IEnumerable<MessagePayload>>.Success(
                [MakeDto(Guid.NewGuid(), userId), MakeDto(Guid.NewGuid(), userId), MakeDto(Guid.NewGuid(), userId)]));

        var query = new MessageQuery
        {
            UserId = userId,
            TenantId = Guid.NewGuid(),
            MessageType = "Info",
            Severity = "Info",
            Status = "New",
            Skip = 1,
            Take = 1,
        };

        var result = await fixture.Service.GetMessages(query, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var command = captured.ShouldBeAssignableTo<IQueryCommand>()!;
        var paging = command.Paging.ShouldNotBeNull();
        paging.Skip.ShouldBe(1);
        paging.Take.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetMessagesWithAfterCursorPutsTheKeysetPredicateOnTheCommand()
    {
        // The window is a predicate over the SORT KEY, not the id: "later timestamp, OR same
        // timestamp and later id". Asserting the command is the only way to see it — the mock
        // gateway applies no window of its own.
        var fixture = CreateService();
        var userId = Guid.NewGuid();
        var cursor = MakeDto(Guid.NewGuid(), userId);
        var commands = new List<IDataCommand>();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand c, DataStoreTarget _, CancellationToken __) => commands.Add(c))
            .ReturnsAsync(GenericResult<IEnumerable<MessagePayload>>.Success([cursor]));

        var result = await fixture.Service.GetMessages(
            new MessageQuery { UserId = userId, After = cursor.Id, Take = 20 },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        // Two reads: the cursor row is resolved first, because its CreatedAt is what the predicate
        // is written against and the caller only named an id.
        commands.Count.ShouldBe(2);
        var windowed = commands[1].ShouldBeAssignableTo<IQueryCommand>()!;
        windowed.Filter.ShouldNotBeNull();
        windowed.Paging.ShouldNotBeNull().Take.ShouldBe(20);
        windowed.Ordering.ShouldNotBeNull().OrderedFields
            .ShouldAllBe(f => f.Direction.Name == "Ascending");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessagesWithBothCursorsFails()
    {
        // Refused rather than resolved by precedence: a caller asking to page forward and backward
        // at once has a bug, and silently honouring one of them hides it.
        var fixture = CreateService();

        var result = await fixture.Service.GetMessages(
            new MessageQuery { UserId = Guid.NewGuid(), After = Guid.NewGuid(), Before = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessagesWhenQueryFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Failure(new GenericMessage("query exploded")));

        var result = await fixture.Service.GetMessages(new MessageQuery { UserId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("query exploded");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessagesWhenQueryFailsWithoutMessagesReturnsMessageQueryFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.GetMessages(new MessageQuery { UserId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessagesWhenValueIsNullReturnsQueryReturnedNullValueFailure()
    {
        var fixture = CreateService();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success(null!));

        var result = await fixture.Service.GetMessages(new MessageQuery { UserId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessagesWhenGatewayThrowsReturnsMessageQueryFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.GetMessages(new MessageQuery { UserId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    // ── GetMessage ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetMessageWithExistingIdReturnsSuccess()
    {
        var fixture = CreateService();
        var id = Guid.NewGuid();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([MakeDto(id, Guid.NewGuid())]));

        var result = await fixture.Service.GetMessage(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessageWithNonExistentIdReturnsMessageNotFoundCode()
    {
        var fixture = CreateService();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success(Array.Empty<MessagePayload>()));

        var result = await fixture.Service.GetMessage(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessageWhenQueryFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.GetMessage(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetMessageWhenGatewayThrowsReturnsMessageQueryFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.GetMessage(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    // ── GetUnreadCount ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetUnreadCountReturnsCountOfMatchingMessages()
    {
        var fixture = CreateService();
        var userId = Guid.NewGuid();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success(
        [
            MakeDto(Guid.NewGuid(), userId),
            MakeDto(Guid.NewGuid(), userId)
        ]));

        var result = await fixture.Service.GetUnreadCount(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetUnreadCountWhenQueryFailsWithoutMessagesReturnsMessageQueryFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.GetUnreadCount(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetUnreadCountWhenGatewayThrowsReturnsMessageQueryFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<MessagePayload>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.GetUnreadCount(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    // ── MarkDelivered ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task MarkDeliveredWithSuccessfulUpdateReturnsSuccess()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var result = await fixture.Service.MarkDelivered(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkDeliveredWhenUpdateFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.MarkDelivered(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkDeliveredWhenUpdateFailsWithoutMessagesReturnsMessageUpdateFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.MarkDelivered(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71002");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkDeliveredWhenGatewayThrowsReturnsMessageUpdateFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.MarkDelivered(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71002");
    }

    // ── MarkRead (+ read-notification fan-out) ─────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task MarkReadWithRecipientNotifiesViaSignalR()
    {
        var fixture = CreateService();
        var messageId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([MakeDto(messageId, recipientId)]));

        var result = await fixture.Service.MarkRead(messageId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.TargetedGroups.ShouldContain(HubScopes.User(recipientId.ToString("D")));
        fixture.Client.Verify(c => c.MessageRead(messageId), Times.Once);
        fixture.Client.Verify(c => c.UnreadCountChanged(), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task MarkReadWithoutRecipientDoesNotNotify()
    {
        var fixture = CreateService();
        var messageId = Guid.NewGuid();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([MakeDto(messageId, recipientUserId: null)]));

        var result = await fixture.Service.MarkRead(messageId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Client.Verify(c => c.MessageRead(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkReadWhenNotificationLookupFailsStillSucceedsWithoutNotifying()
    {
        var fixture = CreateService();
        var messageId = Guid.NewGuid();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success(Array.Empty<MessagePayload>()));

        var result = await fixture.Service.MarkRead(messageId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Client.Verify(c => c.MessageRead(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkReadWhenUpdateFailsWithoutMessagesReturnsMessageUpdateFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.MarkRead(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71002");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkReadWhenGatewayThrowsReturnsMessageUpdateFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.MarkRead(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71002");
    }

    // ── Archive ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ArchiveWithSuccessfulUpdateReturnsSuccess()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var result = await fixture.Service.Archive(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ArchiveWhenUpdateFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.Archive(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ArchiveWhenUpdateFailsWithoutMessagesReturnsMessageUpdateFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.Archive(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71002");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ArchiveWhenGatewayThrowsReturnsMessageUpdateFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.Archive(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71002");
    }

    // ── MarkAllRead ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task MarkAllReadWithSuccessfulUpdateReturnsSuccess()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(3));

        var result = await fixture.Service.MarkAllRead(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkAllReadWhenUpdateFailsWithMessagesPropagatesFailure()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(new GenericMessage("boom")));

        var result = await fixture.Service.MarkAllRead(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkAllReadWhenUpdateFailsWithoutMessagesReturnsMessageUpdateFailedCode()
    {
        var fixture = CreateService();
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Failure(Array.Empty<IGenericMessage>()));

        var result = await fixture.Service.MarkAllRead(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71002");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task MarkAllReadWhenGatewayThrowsReturnsMessageQueryFailedCode()
    {
        var fixture = CreateService();
        fixture.Gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await fixture.Service.MarkAllRead(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    // ── Messages as alerts (FDW-2429) ───────────────────────────────────────

    private static AlertActionRow Dismissed(Guid messageId, Guid userId, DateTimeOffset at)
        => new() { TenantId = Guid.NewGuid(), Kind = "Message", AlertId = messageId, UserId = userId, Action = "Dismiss", UserName = "Ana", ActedAt = at };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AMessageIsAnAlertOfKindMessageThatOffersEveryAction()
    {
        var fixture = CreateService();

        fixture.Service.Kind.ShouldBe("Message");
        fixture.Service.OfferedActions.ShouldBe(["Acknowledge", "Snooze", "Silence", "Dismiss", "Discard"]);
        foreach (var name in fixture.Service.OfferedActions)
            AlertActions.ByName(name).ShouldNotBeSameAs(AlertActions.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task FindAnswersAMessageAddressedToTheCallerWithItsTenant()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInAs(ana);
        var message = MakeDto(Guid.NewGuid(), ana);
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([message]));

        var found = await fixture.Service.Find(message.Id, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value!.Id.ShouldBe(message.Id);
        found.Value.TenantId.ShouldBe(message.TenantId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task FindAnswersNothingForAnotherPersonsMessageABroadcastOrAMissingOne()
    {
        var fixture = CreateService().SignedInAs(Guid.NewGuid());
        var theirs = MakeDto(Guid.NewGuid(), Guid.NewGuid());
        var broadcast = MakeDto(Guid.NewGuid(), null);

        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([theirs]));
        var other = await fixture.Service.Find(theirs.Id, TestContext.Current.CancellationToken);

        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([broadcast]));
        var everyone = await fixture.Service.Find(broadcast.Id, TestContext.Current.CancellationToken);

        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([]));
        var missing = await fixture.Service.Find(Guid.NewGuid(), TestContext.Current.CancellationToken);

        other.IsSuccess.ShouldBeTrue();
        other.Value.ShouldBeNull();
        everyone.IsSuccess.ShouldBeTrue();
        everyone.Value.ShouldBeNull();
        missing.IsSuccess.ShouldBeTrue();
        missing.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task FindAnswersNothingForAPersonWithoutTheRightToReadMessagesEvenForTheirOwnMessage()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInWithoutRights(ana);
        var message = MakeDto(Guid.NewGuid(), ana);
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([message]));

        var found = await fixture.Service.Find(message.Id, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task FindNeedsASignedInCaller()
    {
        var fixture = CreateService();

        var found = await fixture.Service.Find(Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Messages.ShouldContain(m => m.Code == "MESSAGING-51010");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task FindReportsAStoreFailureAndDoesNotCallItNotFound()
    {
        var fixture = CreateService().SignedInAs(Guid.NewGuid());
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Failure(Array.Empty<IGenericMessage>()));

        var found = await fixture.Service.Find(Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Messages.ShouldContain(m => m.Code == "MESSAGING-71001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task APersonalActionMarksAnUnreadMessageRead()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInAs(ana);
        var message = MakeDto(Guid.NewGuid(), ana);
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([message]));
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var acted = await fixture.Service.Acted(message.Id, AlertActions.ByName("Dismiss"), TestContext.Current.CancellationToken);

        acted.IsSuccess.ShouldBeTrue();
        fixture.Client.Verify(c => c.MessageRead(message.Id), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AMessageAlreadyReadKeepsItsReadTimeWhenActedOnAgain()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInAs(ana);
        var message = MakeDto(Guid.NewGuid(), ana);
        message.ReadAt = DateTime.UtcNow.AddHours(-1);
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([message]));
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var acted = await fixture.Service.Acted(message.Id, AlertActions.ByName("Snooze"), TestContext.Current.CancellationToken);

        acted.IsSuccess.ShouldBeTrue();
        fixture.Client.Verify(c => c.MessageRead(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnActionForEveryoneLeavesTheMessageAsItIs()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInAs(ana);
        var message = MakeDto(Guid.NewGuid(), ana);
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([message]));
        SetupExecuteInt(fixture.Gateway, GenericResult<int>.Success(1));

        var acted = await fixture.Service.Acted(message.Id, AlertActions.ByName("Discard"), TestContext.Current.CancellationToken);

        acted.IsSuccess.ShouldBeTrue();
        fixture.Client.Verify(c => c.MessageRead(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ActedOnAMessageThatIsGoneFailsWithNotFound()
    {
        var fixture = CreateService().SignedInAs(Guid.NewGuid());
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([]));

        var acted = await fixture.Service.Acted(Guid.NewGuid(), AlertActions.ByName("Dismiss"), TestContext.Current.CancellationToken);

        acted.IsFailure.ShouldBeTrue();
        acted.Messages.ShouldContain(m => m.Code == "MESSAGING-31000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APageOfMessagesCarriesWhenTheCallerDismissedEachAndNothingForTheOthers()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInAs(ana);
        var dismissed = MakeDto(Guid.NewGuid(), ana);
        var kept = MakeDto(Guid.NewGuid(), ana);
        // A time left on the row by the old column is not the answer: the dismissal is the alert's.
        kept.DismissedAt = DateTime.UtcNow.AddDays(-3);
        var at = new DateTimeOffset(2026, 10, 22, 9, 30, 0, TimeSpan.Zero);
        fixture.Actions.Add(Dismissed(dismissed.Id, ana, at));
        fixture.Actions.Add(new AlertActionRow { Kind = "Message", AlertId = kept.Id, UserId = ana, Action = "Acknowledge", ActedAt = at });
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([dismissed, kept]));

        var page = await fixture.Service.GetMessages(new MessageQuery { UserId = ana }, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue();
        page.Value!.Single(m => m.Id == dismissed.Id).DismissedAt.ShouldBe(at.UtcDateTime);
        page.Value.Single(m => m.Id == kept.Id).DismissedAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task OneMessageCarriesWhenItsRecipientDismissedIt()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInAs(ana);
        var message = MakeDto(Guid.NewGuid(), ana);
        var at = new DateTimeOffset(2026, 10, 22, 9, 30, 0, TimeSpan.Zero);
        fixture.Actions.Add(Dismissed(message.Id, ana, at));
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([message]));

        var opened = await fixture.Service.GetMessage(message.Id, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue();
        opened.Value!.DismissedAt.ShouldBe(at.UtcDateTime);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailureToReadTheDismissalsFailsTheReadAndIsNotHidden()
    {
        var ana = Guid.NewGuid();
        var fixture = CreateService().SignedInAs(ana);
        SetupExecuteMessages(fixture.Gateway, GenericResult<IEnumerable<MessagePayload>>.Success([MakeDto(Guid.NewGuid(), ana)]));
        fixture.Gateway
            .Setup(g => g.Execute<IEnumerable<AlertActionRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<AlertActionRow>>.Failure(new GenericMessage("the alert store is down")));

        var page = await fixture.Service.GetMessages(new MessageQuery { UserId = ana }, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
    }
}
