using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Tests;

/// <summary>
/// Tests for FDW's check of a document link (FDW-3397): a link is stored only when its id is a UUIDv7, its link kind is in force, its target
/// kind is one the application checks and its target is there; the first refusal stops the check.
/// </summary>
public sealed class DocumentLinkRuleTests
{
    private static readonly Guid Rayburn = new("11111111-0000-0000-0000-000000000003");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Site = Guid.CreateVersion7();

    private sealed record Link(Guid Id, string LinkKind, string TargetKind, Guid TargetId) : IDocumentLink
    {
        public Guid OrganizationId => Finance;

        public Guid DocumentId { get; } = Guid.CreateVersion7();
    }

    private sealed record LinkKind(string Code, Guid? TenantId = null, Guid? OrganizationId = null, bool Locked = false) : ILinkKindDefinition
    {
        public Guid Id { get; } = Guid.CreateVersion7();

        public string Name => Code;
    }

    private sealed class Targets(params Guid[] present) : ILinkTargets
    {
        public int ExistsAsked { get; private set; }

        public Task<IGenericResult<IReadOnlyList<string>>> Kinds(Guid organizationId, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IReadOnlyList<string>>.Success([LinkTargetKinds.Document, LinkTargetKinds.Record, "Site"]));

        public Task<IGenericResult<bool>> Exists(Guid organizationId, string targetKind, Guid targetId, CancellationToken cancellationToken = default)
        {
            ExistsAsked++;
            return Task.FromResult(GenericResult<bool>.Success(organizationId == Finance && present.Contains(targetId)));
        }
    }

    private static readonly IReadOnlyList<ILinkKindDefinition> Kinds = [new LinkKind("Replaces"), new LinkKind("Supersedes", Rayburn)];

    private static Task<IGenericResult> Check(Link link, Targets targets) =>
        DocumentLinkRule.Check(link, Rayburn, Kinds, targets, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task ALinkWithAKindInForceAndATargetThatIsThereIsAccepted()
    {
        var result = await Check(new Link(Guid.CreateVersion7(), "supersedes", "site", Site), new Targets(Site));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task ALinkIdThatIsNotAUuidV7IsRefusedBeforeAnythingElseIsAsked()
    {
        var targets = new Targets(Site);

        var result = await Check(new Link(Guid.NewGuid(), "Unknown", "Nowhere", Site), targets);

        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("PublicIdInvalid"));
        targets.ExistsAsked.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task AnUnknownLinkKindIsRefusedAsNotConfigured()
    {
        var targets = new Targets(Site);

        var result = await Check(new Link(Guid.CreateVersion7(), "Explains", "Site", Site), targets);

        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentDefinitionNotConfigured"));
        targets.ExistsAsked.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task AnUnknownTargetKindIsRefusedNamingTheKindsThereAre()
    {
        var targets = new Targets(Site);

        var result = await Check(new Link(Guid.CreateVersion7(), "Replaces", "Planet", Site), targets);

        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentLinkTargetKindUnknown"));
        result.Code.ShouldNotBeNull().FormatMessage(result.Details).ShouldContain("Site");
        targets.ExistsAsked.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public async Task ATargetThatIsNotThereIsRefusedAndNotStored()
    {
        var result = await Check(new Link(Guid.CreateVersion7(), "Replaces", "Document", Guid.CreateVersion7()), new Targets(Site));

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentLinkTargetNotFound"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Validation")]
    public async Task ALinkKindLockedAtThePlatformChecksAgainstThePlatformsDefinition()
    {
        IReadOnlyList<ILinkKindDefinition> kinds = [new LinkKind("Replaces", Locked: true), new LinkKind("Replaces", Rayburn, Finance)];

        var result = await DocumentLinkRule.Check(
            new Link(Guid.CreateVersion7(), "Replaces", "Site", Site), Rayburn, kinds, new Targets(Site), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }
}
