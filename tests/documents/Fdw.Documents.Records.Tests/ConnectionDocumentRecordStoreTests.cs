using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Documents.Records.Tests;

/// <summary>
/// Tests for FDW's one document record store (FDW-3673): each seam call reaches the organization's DocumentRecord connection as its command,
/// a call for another organization or tenant and a command with a problem are refused before anything is sent, and a link passes FDW's link
/// rule first.
/// </summary>
public sealed class ConnectionDocumentRecordStoreTests
{
    private static readonly Guid Rayburn = new("11111111-0000-0000-0000-000000000003");
    private static readonly Guid Acme = new("11111111-0000-0000-0000-000000000004");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");

    private sealed record Document(Guid Id) : IDocumentRecord
    {
        public Guid OrganizationId => Finance;
        public string DocumentType => "Contract";
        public string Title => "Lease";
        public string State => "Draft";
        public DateTimeOffset? EffectiveDate => null;
        public DateTimeOffset UploadedAt => DateTimeOffset.UnixEpoch;
        public Guid? RecordId => null;
    }

    private sealed record Link(Guid Id, string LinkKind) : IDocumentLink
    {
        public Guid OrganizationId => Finance;
        public Guid DocumentId { get; } = Guid.CreateVersion7();
        public string TargetKind => LinkTargetKinds.Document;
        public Guid TargetId { get; } = Guid.CreateVersion7();
    }

    private sealed record LinkKind(string Code) : ILinkKindDefinition
    {
        public Guid Id { get; } = Guid.CreateVersion7();
        public Guid? TenantId => null;
        public Guid? OrganizationId => null;
        public bool Locked => false;
        public string Name => Code;
    }

    private static (ConnectionDocumentRecordStore Store, Mock<IDocumentRecordConnection> Connection) Store()
    {
        var connection = new Mock<IDocumentRecordConnection>();
        connection.Setup(c => c.Execute(It.IsAny<IDocumentRecordCommand<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Success(8));
        connection.Setup(c => c.Execute(It.IsAny<IDocumentRecordCommand<IReadOnlyList<ILinkKindDefinition>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ILinkKindDefinition>>.Success([new LinkKind("Replaces")]));
        connection.Setup(c => c.Execute(It.IsAny<IDocumentRecordCommand<IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<string>>.Success([LinkTargetKinds.Document]));
        connection.Setup(c => c.Execute(It.IsAny<IDocumentRecordCommand<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));
        return (new ConnectionDocumentRecordStore(connection.Object, Finance, Rayburn, NullLogger<ConnectionDocumentRecordStore>.Instance), connection);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Records")]
    public async Task ASaveReachesTheConnectionAsItsCommandWithTheOrganizationDocumentAndRevision()
    {
        var (store, connection) = Store();
        SaveDocumentRecordCommand? sent = null;
        connection.Setup(c => c.Execute(It.IsAny<IDocumentRecordCommand<long>>(), It.IsAny<CancellationToken>()))
            .Callback((IDocumentRecordCommand<long> command, CancellationToken _) => sent = command as SaveDocumentRecordCommand)
            .ReturnsAsync(GenericResult<long>.Success(8));
        var document = new Document(Guid.CreateVersion7());

        var result = await store.Records.Save(Finance, document, 7, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(8);
        var command = sent.ShouldNotBeNull();
        command.OrganizationId.ShouldBe(Finance);
        command.Document.ShouldBeSameAs(document);
        command.ExpectedRevision.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallForAnotherOrganizationIsRefusedAndNothingIsSent()
    {
        var (store, connection) = Store();

        var result = await store.Records.Save(Legal, new Document(Guid.CreateVersion7()), 7, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentRecordCommandRefused"));
        connection.Verify(c => c.Execute(It.IsAny<IDocumentRecordCommand<long>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task DefinitionsOfAnotherTenantAreRefusedAndNothingIsSent()
    {
        var (store, connection) = Store();

        var result = await store.LinkKinds.Rows(Acme, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        connection.Verify(
            c => c.Execute(It.IsAny<IDocumentRecordCommand<IReadOnlyList<ILinkKindDefinition>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task ADocumentWhoseIdIsNotAUuidV7IsRefusedBeforeItIsSent()
    {
        var (store, connection) = Store();

        var result = await store.Records.Save(Finance, new Document(Guid.NewGuid()), 7, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentRecordCommandRefused"));
        connection.Verify(c => c.Execute(It.IsAny<IDocumentRecordCommand<long>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task ALinkOfAKindNotInForceIsRefusedByTheLinkRuleAndNotSent()
    {
        var (store, connection) = Store();

        var result = await store.Links.Add(Finance, new Link(Guid.CreateVersion7(), "Explains"), 7, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentDefinitionNotConfigured"));
        connection.Verify(c => c.Execute(It.IsAny<IDocumentRecordCommand<long>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task ALinkThatPassesTheRuleIsSent()
    {
        var (store, connection) = Store();

        var result = await store.Links.Add(Finance, new Link(Guid.CreateVersion7(), "Replaces"), 7, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        connection.Verify(c => c.Execute(It.IsAny<AddDocumentLinkCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
