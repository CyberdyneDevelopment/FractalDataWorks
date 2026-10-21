using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Capabilities;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Documents.Abstractions.Tests;

/// <summary>Tests for the document command family, the capabilities and the result codes (FDW-1497, FDW-1499, FDW-1500).</summary>
public sealed class DocumentCommandTests
{
    private static DocumentPath Path(string text) => DocumentPath.Parse(text).Value;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandThatNamesTheScopeWhereADocumentIsNeededIsRefusedBeforeAnyStorageIsReached()
    {
        new DeleteCommand(DocumentPath.Root).FirstProblem().ShouldNotBeNull().ShouldContain("scope itself");
        new GetInfoCommand(DocumentPath.Root).FirstProblem().ShouldNotBeNull();
        new DeleteCommand(Path("a.pdf")).FirstProblem().ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARangeOrAPageOrALifetimeThatCannotBeHonouredIsRefused()
    {
        new DownloadCommand(Path("a.pdf"), new DocumentRange(-1, 10)).FirstProblem().ShouldNotBeNull();
        new DownloadCommand(Path("a.pdf"), new DocumentRange(0, 0)).FirstProblem().ShouldNotBeNull();
        new DownloadCommand(Path("a.pdf"), new DocumentRange(0, 10)).FirstProblem().ShouldBeNull();
        new DownloadCommand(Path("a.pdf"), null).FirstProblem().ShouldBeNull();
        new ListCommand(DocumentPath.Root, false, 0, null).FirstProblem().ShouldNotBeNull();
        new ListCommand(DocumentPath.Root, true, 50, null).FirstProblem().ShouldBeNull();
        new CreateReadLinkCommand(Path("a.pdf"), TimeSpan.Zero).FirstProblem().ShouldNotBeNull();
        new CreateReadLinkCommand(Path("a.pdf"), TimeSpan.FromMinutes(5)).FirstProblem().ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACopyOrMoveOntoItselfOrOfTheScopeIsRefused()
    {
        new CopyCommand(Path("a.pdf"), Path("a.pdf"), true).FirstProblem().ShouldNotBeNull().ShouldContain("same path");
        new MoveCommand(DocumentPath.Root, Path("b.pdf"), true).FirstProblem().ShouldNotBeNull();
        new MoveCommand(Path("a.pdf"), Path("b.pdf"), true).FirstProblem().ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUploadStatesItsContentAndKeepsWhetherItIsCreateOnly()
    {
        using var content = new MemoryStream([1, 2, 3]);

        var upload = new UploadCommand(Path("a.pdf"), content, "application/pdf", true, new Dictionary<string, string>());

        upload.FirstProblem().ShouldBeNull();
        upload.CreateOnly.ShouldBeTrue();
        upload.ContentType.ShouldBe("application/pdf");
        Should.Throw<ArgumentNullException>(() => new UploadCommand(Path("a.pdf"), null!, null, false, new Dictionary<string, string>()));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheCapabilitiesAConnectionMayDeclareAreTheNineTheRequestNames()
    {
        DocumentCapabilities.All().Select(capability => capability.Name)
            .ShouldBe(["Metadata", "ContentHash", "Versioning", "RangedReads", "StreamedReads", "ReadLinks", "CreateOnlyWrites", "Copy", "Move"], ignoreOrder: true);
        DocumentCapabilities.ByName("ReadLinks").ShouldBeOfType<ReadLinksCapability>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryAnticipatedFailureHasACodeInItsOwnCategoryAndOnlyTheStorageIsRetryable()
    {
        var codes = DocumentResultCodes.All().ToList();

        codes.Select(code => code.Name).ShouldBe(
            ["DocumentNotFound", "DocumentAlreadyExists", "DocumentOutsideScope", "DocumentPathInvalid", "DocumentTooLarge",
             "DocumentContentTypeNotAllowed", "DocumentReadLinksNotSupported", "DocumentStorageRefused"], ignoreOrder: true);
        codes.Select(code => code.Id).Distinct().Count().ShouldBe(codes.Count);
        (DocumentResultCodes.ByName("DocumentNotFound").Id / 10000).ShouldBe(3);
        (DocumentResultCodes.ByName("DocumentAlreadyExists").Id / 10000).ShouldBe(4);
        (DocumentResultCodes.ByName("DocumentStorageRefused").Id / 10000).ShouldBe(7);
        codes.Where(code => code.IsRetryable).Select(code => code.Name).ShouldBe(["DocumentStorageRefused"]);
    }
}
