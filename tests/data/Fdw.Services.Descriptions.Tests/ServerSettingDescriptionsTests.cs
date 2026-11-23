using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>
/// A server setting's description lives in the descriptions table and not in a column (FDW-6208), and this provider NEVER fills it on load (the description service reads the
/// language through the server settings, so a filled read would read the setting it is reading): a record loaded and saved again carries a null text, and a null text keeps the stored
/// one. A text the save states is written in the unit of the save, a stated blank removes it, and the descriptions go with the setting. The language is resolved before the unit
/// opens (FDW-7256, <see cref="IConfigurationWritePreparer{T}"/>) and the unit's write is handed it, so nothing in the unit reads a setting. The writer is a strict stand-in.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class ServerSettingDescriptionsTests
{
    private static readonly Guid ThingId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);

    private ServerSettingConfigurationProvider Provider()
        => new(NullLogger<ServerSettingConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "settings", "ServerSetting",
            new Lazy<IDescriptionWriter>(() => _writer.Object));

    private static ServerSettingImplementationConfiguration Record(string? text)
        => new() { Id = ThingId, Name = "MaxRows", Description = text };

    private void TheLanguageIs(string language)
        => _writer.Setup(w => w.Language()).Returns(GenericResult<string>.Success(language));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderPreparesAndObservesTheWriteAndTheRemovalButNeverTheRead()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWritePreparer<IServerSettingImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IServerSettingImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IServerSettingImplementationConfiguration>>();
        provider.ShouldNotBeAssignableTo<IConfigurationReadObserver<IServerSettingImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheMemberHasNoInitializerAndNoReadOrSaveSelectsTheColumn()
    {
        new ServerSettingImplementationConfiguration().Description.ShouldBeNull();
        new ServerSettingImplementationConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveThatStatesNoTextAsksForNoLanguageWritesNothingAndSoKeepsTheStoredText()
    {
        // No setup: a call to Language, Describe, Save or Delete fails the test.
        var provider = Provider();
        var record = Record(null);

        var prepared = await provider.Prepare(record, TestContext.Current.CancellationToken);
        var written = await provider.Written(record, null!, TestContext.Current.CancellationToken);

        prepared.IsSuccess.ShouldBeTrue();
        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveThatStatesATextResolvesTheLanguageOnceBeforeTheUnitAndWritesInItWithoutAskingAgain()
    {
        TheLanguageIs("en-US");
        _writer.Setup(w => w.Save(It.Is<DescriptionKey>(k => k.Type == "ServerSetting" && k.SubType == "Text" && k.ThingId == ThingId && k.Language == "en-US"), "MaxRows", null, "A new text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var provider = Provider();
        var record = Record("A new text");

        var prepared = await provider.Prepare(record, TestContext.Current.CancellationToken);
        _writer.Verify(w => w.Language(), Times.Once);
        var written = await provider.Written(record, null!, TestContext.Current.CancellationToken);

        prepared.IsSuccess.ShouldBeTrue();
        written.IsSuccess.ShouldBeTrue();
        // Inside the unit the language is not read again, and the writer's own Describe (which reads it) is never called.
        _writer.Verify(w => w.Language(), Times.Once);
        _writer.Verify(w => w.Describe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
        _writer.VerifyAll();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ASaveThatStatesABlankRemovesTheDescriptionInTheLanguageResolvedBeforeTheUnit(string blank)
    {
        TheLanguageIs("en-US");
        _writer.Setup(w => w.Delete(It.Is<DescriptionKey>(k => k.Type == "ServerSetting" && k.SubType == "Text" && k.ThingId == ThingId && k.Language == "en-US"), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var provider = Provider();
        var record = Record(blank);

        (await provider.Prepare(record, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var written = await provider.Written(record, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.Verify(w => w.Language(), Times.Once);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ALanguageThatIsNotStatedFailsTheSaveBeforeAnyUnitOpensAndWritesNothing()
    {
        _writer.Setup(w => w.Language()).Returns(GenericResult<string>.Failure(new Fdw.Messages.GenericMessage("language not stated")));
        var provider = Provider();

        var prepared = await provider.Prepare(Record("A text"), TestContext.Current.CancellationToken);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        _writer.Verify(w => w.Language(), Times.Once);
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATextSavedIntoAUnitTheCallerOpenedWasNeverPreparedAndFailsLoudWritingNothing()
    {
        var provider = Provider();

        var written = await provider.Written(Record("A text"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        TheLanguageIs("en-US");
        _writer
            .Setup(w => w.Save(It.Is<DescriptionKey>(k => k.Type == "ServerSetting" && k.SubType == "Text" && k.ThingId == ThingId && k.Language == "en-US"), "MaxRows", null, "Text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("part refused")));
        var provider = Provider();
        var record = Record("Text");
        await provider.Prepare(record, TestContext.Current.CancellationToken);

        var written = await provider.Written(record, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("part refused");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARemovedSettingTakesItsDescriptionsWithItInEveryLanguage()
    {
        _writer
            .Setup(w => w.DeleteAbout("ServerSetting", ThingId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(ThingId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }
}
