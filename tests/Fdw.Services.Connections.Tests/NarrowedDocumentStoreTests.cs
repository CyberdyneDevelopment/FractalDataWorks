using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Capabilities;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// Tests for <see cref="NarrowedDocumentStore"/> (FDW-1581): a caller's paths are placed under the folder the store was narrowed to, paths in
/// results come back relative to it, a command the store cannot narrow is refused, and nothing outside the folder is reachable.
/// </summary>
public sealed class NarrowedDocumentStoreTests
{
    private static DocumentPath Path(string text) => DocumentPath.Parse(text).Value;

    private static readonly DocumentPath Folder = Path("rayburn/finance");

    private static DocumentInfo Info(string path) =>
        new(Path(path), 3, "text/plain", DateTimeOffset.UnixEpoch, null, null, new Dictionary<string, string>());

    private sealed class UnknownCommand() : DocumentCommandBase<bool>("Unknown");

    private static NarrowedDocumentStore Store(Mock<IDocumentConnection> connection) =>
        new(connection.Object, Folder, NullLogger<NarrowedDocumentStore>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnUploadIsPlacedUnderTheFolderAndTheAnswerIsRelativeToIt()
    {
        UploadCommand? sent = null;
        var connection = new Mock<IDocumentConnection>();
        connection.Setup(c => c.Execute(It.IsAny<IDocumentCommand<DocumentInfo>>(), It.IsAny<CancellationToken>()))
            .Callback((IDocumentCommand<DocumentInfo> command, CancellationToken _) => sent = command as UploadCommand)
            .ReturnsAsync(GenericResult<DocumentInfo>.Success(Info("rayburn/finance/reports/q1.pdf")));

        var result = await Store(connection).Execute(
            new UploadCommand(Path("reports/q1.pdf"), new MemoryStream(), "application/pdf", true, new Dictionary<string, string>()),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Path.Value.ShouldBe("reports/q1.pdf");
        var upload = sent.ShouldNotBeNull();
        upload.Path.Value.ShouldBe("rayburn/finance/reports/q1.pdf");
        upload.CreateOnly.ShouldBeTrue();
        upload.ContentType.ShouldBe("application/pdf");
    }

    [Theory]
    [InlineData("../other/x.pdf")]
    [InlineData("reports/../../other/x.pdf")]
    [InlineData("..")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void APathThatClimbsOutOfTheFolderCannotEvenBeNamed(string text)
    {
        DocumentPath.Parse(text).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACopyAndAMoveRewriteBothPathsAndNothingReachesASiblingFolder()
    {
        var sent = new List<IDocumentCommand>();
        var connection = new Mock<IDocumentConnection>();
        connection.Setup(c => c.Execute(It.IsAny<IDocumentCommand<DocumentInfo>>(), It.IsAny<CancellationToken>()))
            .Callback((IDocumentCommand<DocumentInfo> command, CancellationToken _) => sent.Add(command))
            .ReturnsAsync(GenericResult<DocumentInfo>.Success(Info("rayburn/finance/b.pdf")));
        var store = Store(connection);

        await store.Execute(new CopyCommand(Path("a.pdf"), Path("b.pdf"), false), TestContext.Current.CancellationToken);
        await store.Execute(new MoveCommand(Path("b.pdf"), Path("archive/b.pdf"), true), TestContext.Current.CancellationToken);

        var copy = sent[0].ShouldBeOfType<CopyCommand>();
        copy.Source.Value.ShouldBe("rayburn/finance/a.pdf");
        copy.Destination.Value.ShouldBe("rayburn/finance/b.pdf");
        var move = sent[1].ShouldBeOfType<MoveCommand>();
        move.Source.Value.ShouldBe("rayburn/finance/b.pdf");
        move.Destination.Value.ShouldBe("rayburn/finance/archive/b.pdf");
        move.CreateOnly.ShouldBeTrue();
        sent.ShouldAllBe(command => command is CopyCommand || command is MoveCommand);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AListIsOfTheFolderAndItsItemsComeBackRelative()
    {
        ListCommand? sent = null;
        var connection = new Mock<IDocumentConnection>();
        connection.Setup(c => c.Execute(It.IsAny<IDocumentCommand<DocumentPage>>(), It.IsAny<CancellationToken>()))
            .Callback((IDocumentCommand<DocumentPage> command, CancellationToken _) => sent = command as ListCommand)
            .ReturnsAsync(GenericResult<DocumentPage>.Success(new DocumentPage([Info("rayburn/finance/a.pdf"), Info("rayburn/finance/reports/b.pdf")], "next")));

        var result = await Store(connection).Execute(new ListCommand(DocumentPath.Root, true, 50, null), TestContext.Current.CancellationToken);

        var list = sent.ShouldNotBeNull();
        list.Folder.Value.ShouldBe("rayburn/finance");
        result.Value!.Items.Select(item => item.Path.Value).ShouldBe(["a.pdf", "reports/b.pdf"]);
        result.Value.Continuation.ShouldBe("next");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnAnswerFromOutsideTheFolderFailsTheResultInsteadOfLeaking()
    {
        var connection = new Mock<IDocumentConnection>();
        connection.Setup(c => c.Execute(It.IsAny<IDocumentCommand<DocumentInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DocumentInfo>.Success(Info("rayburn/legal/secret.pdf")));

        var result = await Store(connection).Execute(new GetInfoCommand(Path("a.pdf")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACommandTheStoreDoesNotKnowIsRefusedAndNeverReachesTheConnection()
    {
        var connection = new Mock<IDocumentConnection>(MockBehavior.Strict);

        var result = await Store(connection).Execute(new UnknownCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AFailureOfTheConnectionIsPassedOnAndTheCapabilitiesAreItsOwn()
    {
        var capability = Mock.Of<IDocumentCapability>(c => c.Name == "ReadLinks");
        var connection = new Mock<IDocumentConnection>();
        connection.SetupGet(c => c.Capabilities).Returns([capability]);
        connection.Setup(c => c.Execute(It.IsAny<IDocumentCommand<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(new GenericMessage("the storage refused")));
        var store = Store(connection);

        var result = await store.Execute(new DeleteCommand(Path("a.pdf")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        store.Capabilities.ShouldHaveSingleItem().Name.ShouldBe("ReadLinks");
        store.Scope.Root.IsRoot.ShouldBeTrue();
    }
}
