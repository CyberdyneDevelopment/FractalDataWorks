using System.Text.Json.Nodes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Tests;

/// <summary>
/// A secret manager's description lives in the descriptions table, never in a secret manager table (FDW-6208), and this domain never fills a record on load: the provider writes a
/// text in the unit of the save (a save that states none keeps the stored one, a stated blank removes it), and the route that shows the text reads it, once, by what the configuration
/// is. The writer and reader are strict stand-ins, so any call a case does not expect is a failure.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class SecretManagerDescriptionsTests
{
    private static readonly Guid ManagerId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    // A body that states a text, as the HashiCorp one does, and one that holds none, as the other kinds do.
    private sealed class DescribedBody : ISecretManagerImplementationConfiguration, IDescribedConfiguration
    {
        public Guid Id { get; set; } = ManagerId;

        public string Name { get; set; } = "vault";

        public string Domain { get; set; } = "SecretManager";

        public string Implementation { get; set; } = "Described";

        public string? Description { get; set; }
    }

    private sealed class PlainBody : ISecretManagerImplementationConfiguration
    {
        public Guid Id { get; set; } = ManagerId;

        public string Name { get; set; } = "env";

        public string Domain { get; set; } = "SecretManager";

        public string Implementation { get; set; } = "Plain";
    }

    private SecretManagerConfigurationProvider Provider()
        => new(NullLogger<SecretManagerConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "sec", "SecretManager", _writer.Object);

    private void ExpectDescribe(string? text)
        => _writer
            .Setup(w => w.Describe("SecretManager", "Text", ManagerId, "vault", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderIsAWriteObserverAndAReferenceProviderAndNeverAReadObserver()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<ISecretManagerImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<ISecretManagerImplementationConfiguration>>();
        provider.ShouldNotBeAssignableTo<IConfigurationReadObserver<ISecretManagerImplementationConfiguration>>();
        provider.ShouldNotBeAssignableTo<IConfigurationChildReadObserver<ISecretManagerImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATextTheSaveStatesIsWrittenInTheUnitOfTheSave()
    {
        ExpectDescribe("The vault the scheduler reads its keys from");

        var written = await Provider().Written(new DescribedBody { Description = "The vault the scheduler reads its keys from" }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AStatedBlankRemovesTheStoredText()
    {
        // The writer's rule: empty or blank text removes the description; the provider passes the stated blank on and does not decide it.
        ExpectDescribe(string.Empty);

        var written = await Provider().Written(new DescribedBody { Description = string.Empty }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARecordReadWithItsTextStoredAndSavedUnchangedKeepsTheTextBecauseNothingIsWritten()
    {
        // A record that was read carries no text (this domain never fills on load): saving it unchanged states none, so the writer is not called at all (strict: any call fails).
        var read = new DescribedBody();

        var written = await Provider().Written(read, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ABodyThatStatesNoDescriptionIsLeftAloneOnASave()
    {
        var written = await Provider().Written(new PlainBody(), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedWriteFailsTheSave()
    {
        _writer
            .Setup(w => w.Describe("SecretManager", "Text", ManagerId, "vault", "a text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(new DescribedBody { Description = "a text" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingASecretManagerRemovesItsDescriptionInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("SecretManager", ManagerId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(ManagerId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheRouteShowsTheStoredTextWithOneReadWhenTheConfigurationStatesOneAndItsSettingsDeclareIt()
    {
        IReadOnlyDictionary<Guid, DescriptionEntry> held = new Dictionary<Guid, DescriptionEntry>
        {
            [ManagerId] = new(new DescriptionKey("SecretManager", "Text", ManagerId, "en-US"), "vault", null, "The vault the scheduler reads its keys from"),
        };
        _reader
            .Setup(r => r.ReadMany("SecretManager", "Text", It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { ManagerId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(held));
        var shown = new JsonObject { ["Address"] = "https://vault", ["Description"] = null };

        var filled = await SecretManagerDescriptions.Fill(shown, new DescribedBody(), _reader.Object, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        shown["Description"]!.GetValue<string>().ShouldBe("The vault the scheduler reads its keys from");
        shown["Address"]!.GetValue<string>().ShouldBe("https://vault");
        _reader.Verify(r => r.ReadMany(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASecretManagerWithNoDescriptionRowKeepsTheEmptyEntryAndNoTextIsMadeUp()
    {
        _reader
            .Setup(r => r.ReadMany("SecretManager", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(new Dictionary<Guid, DescriptionEntry>()));
        var shown = new JsonObject { ["Description"] = null };

        var filled = await SecretManagerDescriptions.Fill(shown, new DescribedBody(), _reader.Object, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        shown["Description"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadFailsTheRouteAndNothingIsShownInPlaceOfTheText()
    {
        _reader
            .Setup(r => r.ReadMany("SecretManager", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new Fdw.Messages.GenericMessage("language not stated")));
        var shown = new JsonObject { ["Description"] = null };

        var filled = await SecretManagerDescriptions.Fill(shown, new DescribedBody(), _reader.Object, TestContext.Current.CancellationToken);

        filled.IsFailure.ShouldBeTrue();
        filled.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        shown["Description"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AConfigurationThatStatesNoDescriptionIsAnsweredWithNoReadAtAll()
    {
        var shown = new JsonObject { ["Description"] = null };

        var filled = await SecretManagerDescriptions.Fill(shown, new PlainBody(), _reader.Object, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        _reader.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task SettingsThatDoNotDeclareADescriptionEntryAreAnsweredWithNoReadAtAll()
    {
        var shown = new JsonObject { ["Address"] = "https://vault" };

        var filled = await SecretManagerDescriptions.Fill(shown, new DescribedBody(), _reader.Object, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        _reader.VerifyNoOtherCalls();
        shown.ContainsKey("Description").ShouldBeFalse();
    }
}
