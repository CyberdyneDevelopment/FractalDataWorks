using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// FDW-2143: <see cref="UiFeedbackService"/>, the UI comments testers pin to a screen. The gateway is a fake that answers every read with the rows it was
/// seeded with and records which container each write touched, so these tests are about what the service decides (what it refuses, who may do what, which
/// tenants a caller reaches, what a list is ordered by), not about the store's filtering.
/// </summary>
public sealed class UiFeedbackServiceTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Ben = Guid.NewGuid();

    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private sealed class StubAccessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            var gateway = new Mock<IDataGateway>(MockBehavior.Loose);
            gateway
                .Setup(g => g.Execute<IEnumerable<UiFeedbackRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<UiFeedbackRow>>.Success(Rows.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<UiFeedbackTagRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<UiFeedbackTagRow>>.Success(Tags.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<UiFeedbackScreenshotRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<UiFeedbackScreenshotRow>>.Success(Pictures.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<UiFeedbackAcknowledgementRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<UiFeedbackAcknowledgementRow>>.Success(Acks.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<UiFeedbackAgreementRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<UiFeedbackAgreementRow>>.Success(Agreements.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<UiFeedbackCommentMarkRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<UiFeedbackCommentMarkRow>>.Success(Comments.Select(c => new UiFeedbackCommentMarkRow { FeedbackId = c.FeedbackId }).ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<UiFeedbackCommentRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<UiFeedbackCommentRow>>.Success(Comments.ToList()));
            gateway
                .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Returns<IDataCommand, DataStoreTarget, CancellationToken>((_, target, _) =>
                {
                    Writes.Add(target.Container);
                    return Task.FromResult<IGenericResult<int>>(
                        Writes.Count == FailWrite ? GenericResult<int>.Failure(Array.Empty<Fdw.Messages.IGenericMessage>()) : GenericResult<int>.Success(UpdatedRows));
                });

            var messaging = new Mock<IMessagingConfigurationProvider>(MockBehavior.Loose);
            messaging
                .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IMessagingImplementationConfiguration>.Success(
                    new MessagingImplementationConfiguration { Name = "Messaging", DataStoreName = "OpsDb", PathName = "msg" }));

            var holders = new Mock<IPermissionHolders>(MockBehavior.Loose);
            holders
                .Setup(h => h.Get(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => TenantLookup ?? GenericResult<IReadOnlyList<Guid>>.Success(TenantReaders));
            holders
                .Setup(h => h.GetOnPlatform(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => PlatformLookup ?? GenericResult<IReadOnlyList<Guid>>.Success(PlatformReaders));

            var messages = new Mock<IMessageService>(MockBehavior.Loose);
            messages
                .Setup(m => m.CreateMessage(It.IsAny<CreateMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateMessageRequest request, CancellationToken _) =>
                {
                    Told.Add(request);
                    return request.RecipientUserId == UnreachableReader
                        ? GenericResult<MessagePayload>.Failure(new GenericMessage("the message store is down"))
                        : GenericResult<MessagePayload>.Success(new MessagePayload { Id = Guid.NewGuid() });
                });
            messages
                .Setup(m => m.GetMessages(It.IsAny<MessageQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IReadOnlyList<MessagePayload>>.Success(Alerts.ToList()));
            messages
                .Setup(m => m.MarkRead(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) =>
                {
                    MarkedRead.Add(id);
                    return GenericResult.Success();
                });

            Service = new UiFeedbackService(
                NullLogger<UiFeedbackService>.Instance, new StubGatewayProvider(gateway.Object), messaging.Object, Accessor, holders.Object, messages.Object);
        }

        public UiFeedbackService Service { get; }

        public StubAccessor Accessor { get; } = new();

        public List<UiFeedbackRow> Rows { get; } = [];

        public List<UiFeedbackTagRow> Tags { get; } = [];

        public List<UiFeedbackScreenshotRow> Pictures { get; } = [];

        public List<UiFeedbackAcknowledgementRow> Acks { get; } = [];

        public List<UiFeedbackAgreementRow> Agreements { get; } = [];

        public List<UiFeedbackCommentRow> Comments { get; } = [];

        /// <summary>The people who hold the read permission in the comment's tenant.</summary>
        public List<Guid> TenantReaders { get; } = [];

        /// <summary>The people who hold the read permission on the platform.</summary>
        public List<Guid> PlatformReaders { get; } = [];

        /// <summary>When set, the lookup of the tenant's readers answers this instead of <see cref="TenantReaders"/>.</summary>
        public IGenericResult<IReadOnlyList<Guid>>? TenantLookup { get; set; }

        /// <summary>When set, the lookup of the platform's readers answers this instead of <see cref="PlatformReaders"/>.</summary>
        public IGenericResult<IReadOnlyList<Guid>>? PlatformLookup { get; set; }

        /// <summary>The one reader whose message fails to be created.</summary>
        public Guid? UnreachableReader { get; set; }

        /// <summary>The messages created to tell readers.</summary>
        public List<CreateMessageRequest> Told { get; } = [];

        /// <summary>The alerts the message store holds for the caller about a comment.</summary>
        public List<MessagePayload> Alerts { get; } = [];

        /// <summary>The messages marked read.</summary>
        public List<Guid> MarkedRead { get; } = [];

        /// <summary>The container each write touched, in order.</summary>
        public List<string> Writes { get; } = [];

        /// <summary>How many rows a write reports changing.</summary>
        public int UpdatedRows { get; set; } = 1;

        /// <summary>The number (from 1) of the write that fails; 0 for none.</summary>
        public int FailWrite { get; set; }

        public Fixture SignedInAs(Guid userId, Guid tenantId, string name, bool globalTenant = false)
            => SignedIn(userId, tenantId, name, globalTenant, ["ui-feedback:read"]);

        /// <summary>Signs in a reviewer: someone who holds the write right and not the read right.</summary>
        public Fixture SignedInAsReviewer(Guid userId, Guid tenantId, string name)
            => SignedIn(userId, tenantId, name, false, ["ui-feedback:write"]);

        private Fixture SignedIn(Guid userId, Guid tenantId, string name, bool globalTenant, string[] permissions)
        {
            var context = new Mock<IAuthenticationContext>(MockBehavior.Loose);
            context.SetupGet(c => c.IsAuthenticated).Returns(true);
            context.SetupGet(c => c.UserId).Returns(userId.ToString("D"));
            context.SetupGet(c => c.Username).Returns(name);
            context.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
            context.SetupGet(c => c.IsSystemContext).Returns(false);
            context.SetupGet(c => c.Permissions).Returns(permissions);
            context.SetupGet(c => c.Claims).Returns(globalTenant
                ? new Dictionary<string, object> { [ClaimDefinitions.globalTenant.Name] = "true" }
                : new Dictionary<string, object>());
            Accessor.Current = context.Object;
            return this;
        }

        public UiFeedbackRow Add(Guid tenantId, Guid authorId, string urgency = "Normal", string state = "Open", int minutesAgo = 0, bool hasScreenshot = false)
        {
            var row = new UiFeedbackRow
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AuthorUserId = authorId,
                AuthorName = authorId == Ana ? "Ana" : "Ben",
                Screen = "https://console/datasets/Teams",
                Route = "/datasets/$name",
                AreaX = 10,
                AreaY = 20,
                AreaW = 30,
                AreaH = 40,
                ViewportW = 1280,
                ViewportH = 720,
                Body = "The totals column is cut off",
                Urgency = urgency,
                State = state,
                ResolvedAt = state == "Resolved" ? DateTimeOffset.UtcNow : null,
                ResolvedByName = state == "Resolved" ? "Ben" : null,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo),
                HasScreenshot = hasScreenshot
            };
            Rows.Add(row);
            return row;
        }
    }

    private static Fixture AnaInA() => new Fixture().SignedInAs(Ana, TenantA, "Ana");

    private static CreateUiFeedbackRequest ValidRequest()
        => new()
        {
            Screen = "https://console/datasets/Teams",
            Route = "/datasets/$name",
            Area = new UiFeedbackArea { X = 10, Y = 20, W = 30, H = 40 },
            Viewport = new UiFeedbackSize { W = 1280, H = 720 },
            Theme = "dark",
            AnchorJson = "{\"role\":\"table\"}",
            Urgency = "High",
            Tags = ["UsabilityIssue", "CalculationIssue"],
            Body = "The totals column is cut off"
        };

    private static string PngDataUrl(int length)
    {
        var bytes = new byte[length];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CreateKeepsTheCommentItsTagsAndItsPicture()
    {
        var fixture = AnaInA();
        var request = ValidRequest();
        request.Screenshot = PngDataUrl(64);

        var result = await fixture.Service.Create(request, Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["UiFeedback", "UiFeedbackTag", "UiFeedbackTag", "UiFeedbackScreenshot"]);
        result.Value!.State.ShouldBe("Open");
        result.Value.AcknowledgedByMe.ShouldBeFalse();
        result.Value.IsMine.ShouldBeTrue();
        result.Value.AuthorName.ShouldBe("Ana");
        result.Value.Urgency.ShouldBe("High");
        result.Value.Tags.ShouldBe(["CalculationIssue", "UsabilityIssue"]);
        result.Value.HasScreenshot.ShouldBeTrue();
        result.Value.AnchorJson.ShouldBe("{\"role\":\"table\"}");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("screen-blank", "screen", "MESSAGING-21000")]
    [InlineData("route-blank", "route", "MESSAGING-21000")]
    [InlineData("area-missing", "area", "MESSAGING-21000")]
    [InlineData("area-negative", "area", "MESSAGING-21001")]
    [InlineData("viewport-missing", "viewport", "MESSAGING-21000")]
    [InlineData("viewport-zero", "viewport", "MESSAGING-21001")]
    [InlineData("theme-blank", "theme", "MESSAGING-21001")]
    [InlineData("anchor-not-json", "anchor", "MESSAGING-21001")]
    [InlineData("urgency-missing", "urgency", "MESSAGING-21000")]
    [InlineData("urgency-unknown", "urgency", "MESSAGING-21001")]
    [InlineData("tag-unknown", "tag", "MESSAGING-21001")]
    [InlineData("tag-twice", "tags", "MESSAGING-21001")]
    [InlineData("tags-missing", "tags", "MESSAGING-21001")]
    [InlineData("body-blank", "body", "MESSAGING-21000")]
    [InlineData("body-long", "body", "MESSAGING-21001")]
    [InlineData("shot-not-a-data-url", "screenshot", "MESSAGING-21001")]
    [InlineData("shot-not-png", "screenshot", "MESSAGING-21001")]
    [InlineData("shot-too-big", "screenshot", "MESSAGING-21001")]
    public async Task CreateRefusesAWrongFieldByNamingItAndKeepsNothing(string mistake, string field, string code)
    {
        var fixture = AnaInA();
        var request = ValidRequest();
        switch (mistake)
        {
            case "screen-blank": request.Screen = " "; break;
            case "route-blank": request.Route = null; break;
            case "area-missing": request.Area = null; break;
            case "area-negative": request.Area = new UiFeedbackArea { X = 1, Y = 1, W = -5, H = 2 }; break;
            case "viewport-missing": request.Viewport = null; break;
            case "viewport-zero": request.Viewport = new UiFeedbackSize { W = 0, H = 700 }; break;
            case "theme-blank": request.Theme = "  "; break;
            case "anchor-not-json": request.AnchorJson = "{not json"; break;
            case "urgency-missing": request.Urgency = null; break;
            case "urgency-unknown": request.Urgency = "Whenever"; break;
            case "tag-unknown": request.Tags = ["Annoying"]; break;
            case "tag-twice": request.Tags = ["UsabilityIssue", "UsabilityIssue"]; break;
            case "tags-missing": request.Tags = null; break;
            case "body-blank": request.Body = ""; break;
            case "body-long": request.Body = new string('x', UiFeedbackLimits.MaxBodyLength + 1); break;
            case "shot-not-a-data-url": request.Screenshot = "https://elsewhere/shot.png"; break;
            case "shot-not-png": request.Screenshot = "data:image/png;base64," + Convert.ToBase64String(new byte[32]); break;
            case "shot-too-big": request.Screenshot = PngDataUrl(UiFeedbackLimits.MaxPictureBytes + 10); break;
            default: throw new InvalidOperationException(mistake);
        }

        var result = await fixture.Service.Create(request, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == code);
        result.CurrentMessage!.ShouldContain(field);
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateNeedsASignedInCaller()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.Create(ValidRequest(), Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-51001");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateTakesBackTheRowsAlreadyWrittenWhenALaterOneFails()
    {
        var fixture = AnaInA();
        fixture.FailWrite = 2;
        var request = ValidRequest();
        request.Screenshot = PngDataUrl(64);

        var result = await fixture.Service.Create(request, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71004");
        fixture.Writes.ShouldBe(["UiFeedback", "UiFeedbackTag", "UiFeedbackComment", "UiFeedbackAgreement", "UiFeedbackAcknowledgement", "UiFeedbackTag", "UiFeedbackScreenshot", "UiFeedback"]);
    }

    // ── Telling the readers ─────────────────────────────────────────────────

    private static readonly Guid Cy = Guid.NewGuid();
    private static readonly Guid Dee = Guid.NewGuid();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SavingACommentTellsEachReaderOnceExceptItsAuthor()
    {
        var fixture = AnaInA();
        fixture.TenantReaders.AddRange([Ana, Ben, Cy]);
        fixture.PlatformReaders.AddRange([Cy, Dee]);

        var result = await fixture.Service.Create(ValidRequest(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldBeEmpty();
        fixture.Told.Select(m => m.RecipientUserId).ShouldBe([Ben, Cy, Dee], ignoreOrder: true);
        var alert = fixture.Told.First();
        alert.MessageType.ShouldBe("UiFeedback");
        alert.SenderUserId.ShouldBe(Ana);
        alert.TenantId.ShouldBe(TenantA);
        alert.ReferenceId.ShouldBe(result.Value!.Id.ToString("D"));
        alert.ActionUrl.ShouldBe($"/feedback/{result.Value.Id:D}");
        alert.Subject.ShouldBe("Ana left a UI comment on /datasets/$name");
        alert.Severity.ShouldBe("Warning");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ASecurityIssueIsToldAsAnError()
    {
        var fixture = AnaInA();
        fixture.TenantReaders.Add(Ben);
        var request = ValidRequest();
        request.Urgency = "SecurityIssue";

        await fixture.Service.Create(request, Ct);

        fixture.Told.Single().Severity.ShouldBe("Error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReaderWhoCannotBeToldIsReportedAndTheCommentAndTheOtherReadersAreKept()
    {
        var fixture = AnaInA();
        fixture.TenantReaders.AddRange([Ben, Cy]);
        fixture.UnreachableReader = Ben;

        var result = await fixture.Service.Create(ValidRequest(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71005");
        fixture.Writes.ShouldContain("UiFeedback");
        fixture.Told.Select(m => m.RecipientUserId).ShouldContain(Cy);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadersThatCannotBeFoundAreReportedAndTheCommentIsKept()
    {
        var fixture = AnaInA();
        fixture.TenantLookup = GenericResult<IReadOnlyList<Guid>>.Failure(new GenericMessage("the roles are unreadable"));
        fixture.PlatformReaders.Add(Dee);

        var result = await fixture.Service.Create(ValidRequest(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71006");
        fixture.Told.Select(m => m.RecipientUserId).ShouldBe([Dee]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task APlatformLookupThatFailsIsReportedAndTheTenantsReadersAreStillTold()
    {
        var fixture = AnaInA();
        fixture.TenantReaders.Add(Ben);
        fixture.PlatformLookup = GenericResult<IReadOnlyList<Guid>>.Failure(new GenericMessage("the tenants are unreadable"));

        var result = await fixture.Service.Create(ValidRequest(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-71006");
        fixture.Told.Select(m => m.RecipientUserId).ShouldBe([Ben]);
    }

    // ── Acknowledge ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AcknowledgeKeepsWhoAndWhenAndMarksTheCallersAlertRead()
    {
        var fixture = new Fixture().SignedInAs(Ben, TenantA, "Ben");
        var comment = fixture.Add(TenantA, Ana, "SecurityIssue");
        var alert = new MessagePayload { Id = Guid.NewGuid(), MessageType = "UiFeedback", ReferenceId = comment.Id.ToString("D") };
        fixture.Alerts.Add(alert);

        var result = await fixture.Service.Acknowledge(comment.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["UiFeedbackAcknowledgement"]);
        fixture.MarkedRead.ShouldBe([alert.Id]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AcknowledgingAgainChangesNothing()
    {
        var fixture = new Fixture().SignedInAs(Ben, TenantA, "Ben");
        var comment = fixture.Add(TenantA, Ana, "SecurityIssue");
        fixture.Acks.Add(new UiFeedbackAcknowledgementRow { FeedbackId = comment.Id, UserId = Ben, UserName = "Ben", AcknowledgedAt = DateTimeOffset.UtcNow });

        var result = await fixture.Service.Acknowledge(comment.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.AcknowledgedByMe.ShouldBeTrue();
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ACommentOutOfReachCannotBeAcknowledged()
    {
        var fixture = new Fixture().SignedInAs(Ben, TenantA, "Ben");
        var theirs = fixture.Add(TenantB, Cy);

        var result = await fixture.Service.Acknowledge(theirs.Id, Ct);

        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task EveryAnswerSaysWhetherTheCallerHasAcknowledged()
    {
        var fixture = new Fixture().SignedInAs(Ben, TenantA, "Ben");
        var acknowledged = fixture.Add(TenantA, Ana, "SecurityIssue");
        var pending = fixture.Add(TenantA, Ana, "SecurityIssue");
        fixture.Acks.Add(new UiFeedbackAcknowledgementRow { FeedbackId = acknowledged.Id, UserId = Ben, UserName = "Ben", AcknowledgedAt = DateTimeOffset.UtcNow });

        var listed = await fixture.Service.List(new UiFeedbackQuery(), Ct);
        var opened = await fixture.Service.Get(pending.Id, Ct);

        listed.Value!.Single(c => c.Id == acknowledged.Id).AcknowledgedByMe.ShouldBeTrue();
        listed.Value.Single(c => c.Id == pending.Id).AcknowledgedByMe.ShouldBeFalse();
        opened.Value!.AcknowledgedByMe.ShouldBeFalse();
    }

    // ── Reach ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ACommentOfAnotherTenantDoesNotExistForTheCaller()
    {
        var fixture = AnaInA();
        var theirs = fixture.Add(TenantB, Ben);

        var result = await fixture.Service.Get(theirs.Id, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task APlatformAdministratorReachesEveryTenantsComments()
    {
        var fixture = new Fixture().SignedInAs(Ana, TenantA, "Ana", globalTenant: true);
        var theirs = fixture.Add(TenantB, Ben);

        var result = await fixture.Service.Get(theirs.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsMine.ShouldBeFalse();
        result.Value.AuthorName.ShouldBe("Ben");
    }

    // ── Edit and delete are the author's ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task OnlyTheAuthorCanEditOrDeleteAComment()
    {
        var fixture = AnaInA();
        var bens = fixture.Add(TenantA, Ben);

        var edited = await fixture.Service.Edit(bens.Id, "new note", Ct);
        var deleted = await fixture.Service.Delete(bens.Id, Ct);

        edited.Messages.ShouldContain(m => m.Code == "MESSAGING-100000");
        deleted.Messages.ShouldContain(m => m.Code == "MESSAGING-100000");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task EditChangesTheNoteOfTheAuthorsOwnComment()
    {
        var fixture = AnaInA();
        var mine = fixture.Add(TenantA, Ana);

        var result = await fixture.Service.Edit(mine.Id, "clearer note", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Body.ShouldBe("clearer note");
        result.Value.UpdatedAt.ShouldNotBeNull();
        fixture.Writes.ShouldBe(["UiFeedback"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task EditRefusesABlankNoteBeforeReadingAnything()
    {
        var fixture = AnaInA();
        var mine = fixture.Add(TenantA, Ana);

        var result = await fixture.Service.Edit(mine.Id, "  ", Ct);

        result.Messages.ShouldContain(m => m.Code == "MESSAGING-21000");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task DeleteRemovesTheAcknowledgementsTagsAndPictureBeforeTheComment()
    {
        var fixture = AnaInA();
        var mine = fixture.Add(TenantA, Ana, hasScreenshot: true);

        var result = await fixture.Service.Delete(mine.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["UiFeedbackComment", "UiFeedbackAgreement", "UiFeedbackAcknowledgement", "UiFeedbackTag", "UiFeedbackScreenshot", "UiFeedback"]);
    }

    // ── Resolve and reopen ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ResolveRecordsWhoResolvedItAndWhen()
    {
        var fixture = AnaInA();
        var open = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.Resolve(open.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.State.ShouldBe("Resolved");
        result.Value.ResolvedByName.ShouldBe("Ana");
        result.Value.ResolvedAt.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ReopenClearsWhoResolvedItAndWhen()
    {
        var fixture = AnaInA();
        var resolved = fixture.Add(TenantA, Ben, state: "Resolved");

        var result = await fixture.Service.Reopen(resolved.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.State.ShouldBe("Open");
        result.Value.ResolvedByName.ShouldBeNull();
        result.Value.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommentAlreadyInTheStateAskedForIsAConflict()
    {
        var fixture = AnaInA();
        var open = fixture.Add(TenantA, Ben);
        var resolved = fixture.Add(TenantA, Ben, state: "Resolved");

        var resolveOpenAgain = await fixture.Service.Resolve(resolved.Id, Ct);
        var reopenOpen = await fixture.Service.Reopen(open.Id, Ct);

        resolveOpenAgain.Messages.ShouldContain(m => m.Code == "MESSAGING-41000");
        reopenOpen.Messages.ShouldContain(m => m.Code == "MESSAGING-41000");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommentSomeoneElseMovedFirstIsAConflictAndNotASecondChange()
    {
        var fixture = AnaInA();
        var open = fixture.Add(TenantA, Ben);
        fixture.UpdatedRows = 0;

        var result = await fixture.Service.Resolve(open.Id, Ct);

        result.Messages.ShouldContain(m => m.Code == "MESSAGING-41000");
    }

    // ── List ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ListByUrgencyPutsTheMostUrgentFirstThenTheNewest()
    {
        var fixture = AnaInA();
        fixture.Add(TenantA, Ben, "Low", minutesAgo: 1);
        fixture.Add(TenantA, Ben, "SecurityIssue", minutesAgo: 90);
        fixture.Add(TenantA, Ben, "High", minutesAgo: 30);
        fixture.Add(TenantA, Ben, "High", minutesAgo: 5);

        var result = await fixture.Service.List(new UiFeedbackQuery { SortByUrgency = true }, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(c => c.Urgency).ShouldBe(["SecurityIssue", "High", "High", "Low"]);
        result.Value[1].CreatedAt.ShouldBeGreaterThan(result.Value[2].CreatedAt);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ListByTagKeepsOnlyTheCommentsCarryingIt()
    {
        var fixture = AnaInA();
        var tagged = fixture.Add(TenantA, Ben);
        fixture.Add(TenantA, Ben);
        fixture.Tags.Add(new UiFeedbackTagRow { FeedbackId = tagged.Id, Tag = "FeatureRequest" });

        var result = await fixture.Service.List(new UiFeedbackQuery { Tag = "FeatureRequest" }, Ct);

        result.Value!.Select(c => c.Id).ShouldBe([tagged.Id]);
        result.Value[0].Tags.ShouldBe(["FeatureRequest"]);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("take-zero", "take")]
    [InlineData("take-over", "take")]
    [InlineData("skip-negative", "skip")]
    [InlineData("state-unknown", "state")]
    [InlineData("urgency-unknown", "urgency")]
    [InlineData("tag-unknown", "tag")]
    [InlineData("route-blank", "route")]
    public async Task ListRefusesAConditionItCannotUseByNamingIt(string mistake, string field)
    {
        var fixture = AnaInA();
        var query = new UiFeedbackQuery();
        switch (mistake)
        {
            case "take-zero": query.Take = 0; break;
            case "take-over": query.Take = UiFeedbackLimits.MaxPageSize + 1; break;
            case "skip-negative": query.Skip = -1; break;
            case "state-unknown": query.State = "Closed"; break;
            case "urgency-unknown": query.Urgency = "Whenever"; break;
            case "tag-unknown": query.Tag = "Annoying"; break;
            case "route-blank": query.Route = " "; break;
            default: throw new InvalidOperationException(mistake);
        }

        var result = await fixture.Service.List(query, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-21001");
        result.CurrentMessage!.ShouldContain(field);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task OrderingByAnUrgencyNoLongerDeclaredFailsInsteadOfGuessingItsRank()
    {
        var fixture = AnaInA();
        fixture.Add(TenantA, Ben, "Whenever");

        var result = await fixture.Service.List(new UiFeedbackQuery { SortByUrgency = true }, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AListThatWouldIncludeAnotherTenantsCommentFailsInsteadOfShowingIt()
    {
        // The test gateway ignores filters, as a store query with no tenant would: the service must not trust the query alone.
        var fixture = AnaInA();
        var mine = fixture.Add(TenantA, Ben);
        var theirs = fixture.Add(TenantB, Cy);

        var result = await fixture.Service.List(new UiFeedbackQuery(), Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91001");
        result.CurrentMessage!.ShouldContain(theirs.Id.ToString("D"));
        result.CurrentMessage!.ShouldNotContain(mine.Id.ToString("D"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task APlatformAdministratorListsEveryTenantsComments()
    {
        var fixture = new Fixture().SignedInAs(Ana, TenantA, "Ana", globalTenant: true);
        fixture.Add(TenantA, Ben);
        fixture.Add(TenantB, Cy);

        var result = await fixture.Service.List(new UiFeedbackQuery(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
    }

    // ── Picture ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ThePictureIsServedAsItWasKept()
    {
        var fixture = AnaInA();
        var withPicture = fixture.Add(TenantA, Ben, hasScreenshot: true);
        fixture.Pictures.Add(new UiFeedbackScreenshotRow { FeedbackId = withPicture.Id, ContentType = "image/png", Picture = [1, 2, 3] });

        var result = await fixture.Service.GetPicture(withPicture.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ContentType.ShouldBe("image/png");
        result.Value.Content.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ACommentSavedWithoutAPictureHasNoneToServe()
    {
        var fixture = AnaInA();
        var without = fixture.Add(TenantA, Ben, hasScreenshot: false);

        var result = await fixture.Service.GetPicture(without.Id, Ct);

        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
    }

    // ── A reviewer sees the comments that are not security issues (FDW-2246) ──

    private static Fixture ReviewerAna() => new Fixture().SignedInAsReviewer(Ana, TenantA, "Ana");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerListsAnotherPersonsNonSecurityCommentAndTheirOwnSecurityComment()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben, urgency: "High");
        var mine = fixture.Add(TenantA, Ana, urgency: "SecurityIssue", minutesAgo: 5);

        var result = await fixture.Service.List(new UiFeedbackQuery(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(c => c.Id).ShouldBe([theirs.Id, mine.Id]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AListThatWouldShowAReviewerAnotherPersonsSecurityIssueFailsInsteadOfShowingIt()
    {
        var fixture = ReviewerAna();
        fixture.Add(TenantA, Ben, urgency: "SecurityIssue");

        var result = await fixture.Service.List(new UiFeedbackQuery(), Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReadHolderListsEverySecurityIssueToo()
    {
        var fixture = AnaInA();
        var security = fixture.Add(TenantA, Ben, urgency: "SecurityIssue");

        var result = await fixture.Service.List(new UiFeedbackQuery(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(c => c.Id).ShouldBe([security.Id]);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("Low")]
    [InlineData("Normal")]
    [InlineData("High")]
    public async Task AReviewerOpensAnotherPersonsNonSecurityComment(string urgency)
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben, urgency: urgency);

        var result = await fixture.Service.Get(theirs.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsMine.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerCannotOpenAnotherPersonsSecurityIssueAndItDoesNotExistForThem()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben, urgency: "SecurityIssue");

        var result = await fixture.Service.Get(theirs.Id, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerCannotReadThePictureOfAnotherPersonsSecurityIssue()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben, urgency: "SecurityIssue", hasScreenshot: true);

        var result = await fixture.Service.GetPicture(theirs.Id, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerOpensTheirOwnSecurityIssue()
    {
        var fixture = ReviewerAna();
        var mine = fixture.Add(TenantA, Ana, urgency: "SecurityIssue");

        var result = await fixture.Service.Get(mine.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerDoesNotSeeAnotherCompanysNonSecurityComment()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantB, Ben, urgency: "Normal");

        var result = await fixture.Service.Get(theirs.Id, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUrgencyTheCollectionDoesNotDeclareFailsTheReviewersReadInsteadOfBeingGuessedAt()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben, urgency: "Removed");

        var result = await fixture.Service.Get(theirs.Id, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-91001");
    }

    // ── Agreeing (FDW-2247) ─────────────────────────────────────────────────

    private static UiFeedbackAgreementRow AgreementOf(UiFeedbackRow comment, Guid person)
        => new() { FeedbackId = comment.Id, UserId = person, UserName = person == Ana ? "Ana" : "Ben", AgreedAt = DateTimeOffset.UtcNow };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AReviewerAgreesWithAnotherPersonsComment()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.Agree(theirs.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["UiFeedbackAgreement"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AgreeingAgainChangesNothingAndStillSucceeds()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);
        fixture.Agreements.Add(AgreementOf(theirs, Ana));

        var result = await fixture.Service.Agree(theirs.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBeEmpty();
        result.Value!.AgreedByMe.ShouldBeTrue();
        result.Value.AgreeCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ThePersonWhoLeftACommentCannotAgreeWithIt()
    {
        var fixture = ReviewerAna();
        var mine = fixture.Add(TenantA, Ana);

        var result = await fixture.Service.Agree(mine.Id, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-41001");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerCannotAgreeWithACommentTheyMayNotSee()
    {
        var fixture = ReviewerAna();
        var security = fixture.Add(TenantA, Ben, urgency: "SecurityIssue");
        var otherCompany = fixture.Add(TenantB, Ben);

        var first = await fixture.Service.Agree(security.Id, Ct);
        var second = await fixture.Service.Agree(otherCompany.Id, Ct);

        first.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
        second.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACommentCarriesHowManyPeopleAgreeAndWhetherTheCallerDoes()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);
        var other = fixture.Add(TenantA, Ben, minutesAgo: 5);
        fixture.Agreements.Add(AgreementOf(theirs, Ana));
        fixture.Agreements.Add(AgreementOf(theirs, Guid.NewGuid()));
        fixture.Agreements.Add(AgreementOf(other, Guid.NewGuid()));

        var listed = await fixture.Service.List(new UiFeedbackQuery(), Ct);
        var opened = await fixture.Service.Get(theirs.Id, Ct);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Single(c => c.Id == theirs.Id).AgreeCount.ShouldBe(2);
        listed.Value!.Single(c => c.Id == theirs.Id).AgreedByMe.ShouldBeTrue();
        listed.Value!.Single(c => c.Id == other.Id).AgreedByMe.ShouldBeFalse();
        opened.Value!.AgreeCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AReviewerTakesBackTheirAgreement()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.TakeBackAgreement(theirs.Id, Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["UiFeedbackAgreement"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerCannotTakeBackAnAgreementOnACommentTheyMayNotSee()
    {
        var fixture = ReviewerAna();
        var security = fixture.Add(TenantA, Ben, urgency: "SecurityIssue");

        var result = await fixture.Service.TakeBackAgreement(security.Id, Ct);

        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedAgreementWriteIsReportedWhenThePersonDoesNotAgreeAfterwards()
    {
        var fixture = ReviewerAna();
        fixture.FailWrite = 1;
        var theirs = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.Agree(theirs.Id, Ct);

        result.IsFailure.ShouldBeTrue();
    }

    // ── Comments on a note (FDW-2248) ───────────────────────────────────────

    private static UiFeedbackCommentRow CommentOn(UiFeedbackRow note, Guid person, string body, int minutesAgo = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            FeedbackId = note.Id,
            TenantId = note.TenantId,
            AuthorUserId = person,
            AuthorName = person == Ana ? "Ana" : "Ben",
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo)
        };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AReviewerCommentsOnAnotherPersonsNote()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.AddComment(theirs.Id, "Same here on the totals column", Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["UiFeedbackComment"]);
        result.Value!.FeedbackId.ShouldBe(theirs.Id);
        result.Value.AuthorUserId.ShouldBe(Ana);
        result.Value.AuthorName.ShouldBe("Ana");
        result.Value.Body.ShouldBe("Same here on the totals column");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null, "MESSAGING-21000")]
    [InlineData("   ", "MESSAGING-21000")]
    public async Task ACommentWithNoTextIsRefusedNamingTheField(string? body, string code)
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.AddComment(theirs.Id, body, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == code);
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommentLongerThanTheLimitIsRefusedNotCut()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.AddComment(theirs.Id, new string('x', UiFeedbackLimits.MaxBodyLength + 1), Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-21001");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerCannotCommentOnANoteTheyMayNotSee()
    {
        var fixture = ReviewerAna();
        var security = fixture.Add(TenantA, Ben, urgency: "SecurityIssue");
        var otherCompany = fixture.Add(TenantB, Ben);

        var first = await fixture.Service.AddComment(security.Id, "Seen this too", Ct);
        var second = await fixture.Service.AddComment(otherCompany.Id, "Seen this too", Ct);

        first.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
        second.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARefusalOfTheTextDoesNotTellAPersonWhoMayNotSeeTheNoteAnything()
    {
        var fixture = ReviewerAna();
        var security = fixture.Add(TenantA, Ben, urgency: "SecurityIssue");

        var result = await fixture.Service.AddComment(security.Id, null, Ct);

        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheCommentsOnANoteAreListed()
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);
        var newer = CommentOn(theirs, Ben, "second", minutesAgo: 1);
        var older = CommentOn(theirs, Ana, "first", minutesAgo: 5);
        fixture.Comments.Add(newer);
        fixture.Comments.Add(older);

        var result = await fixture.Service.ListComments(theirs.Id, 0, 50, Ct);

        result.IsSuccess.ShouldBeTrue();
        // The order is the store's ORDER BY (CreatedAt, Id); the fake gateway ignores it, so this shows the service returns every comment of the note.
        result.Value!.Select(c => c.Body).ShouldBe(["second", "first"], ignoreOrder: true);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(-1, 50)]
    [InlineData(0, 0)]
    [InlineData(0, 201)]
    public async Task AWindowOutsideTheLimitsIsRefused(int skip, int take)
    {
        var fixture = ReviewerAna();
        var theirs = fixture.Add(TenantA, Ben);

        var result = await fixture.Service.ListComments(theirs.Id, skip, take, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "MESSAGING-21001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReviewerCannotListTheCommentsOnANoteTheyMayNotSee()
    {
        var fixture = ReviewerAna();
        var security = fixture.Add(TenantA, Ben, urgency: "SecurityIssue");

        var result = await fixture.Service.ListComments(security.Id, 0, 50, Ct);

        result.Messages.ShouldContain(m => m.Code == "MESSAGING-31004");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ANoteCarriesHowManyCommentsItHas()
    {
        var fixture = ReviewerAna();
        var busy = fixture.Add(TenantA, Ben);
        var quiet = fixture.Add(TenantA, Ben, minutesAgo: 5);
        fixture.Comments.Add(CommentOn(busy, Ben, "one"));
        fixture.Comments.Add(CommentOn(busy, Ana, "two"));

        var listed = await fixture.Service.List(new UiFeedbackQuery(), Ct);
        var opened = await fixture.Service.Get(busy.Id, Ct);

        listed.Value!.Single(c => c.Id == busy.Id).CommentCount.ShouldBe(2);
        listed.Value!.Single(c => c.Id == quiet.Id).CommentCount.ShouldBe(0);
        opened.Value!.CommentCount.ShouldBe(2);
    }
}
