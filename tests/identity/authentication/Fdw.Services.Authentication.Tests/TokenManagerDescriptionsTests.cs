using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.TokenManagers;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests;

/// <summary>
/// A token manager's description lives in the descriptions table and not in a column of its own table (FDW-6208), and this provider NEVER fills it on load (a token manager is read when a token is issued, and no route shows its text): a record
/// loaded and saved again carries a null text, and a null text keeps the stored one. A text the save states is written in the unit of the save, a stated blank removes it,
/// and the descriptions go with the thing. The writer is a strict stand-in (a call nobody set up fails the test); <c>DescriptionServiceTests</c> covers what the real one does.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class TokenManagerDescriptionsTests
{
    private static readonly Guid ThingId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);

    private TokenManagerConfigurationProvider Provider()
        => new(NullLogger<TokenManagerConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "auth", "TokenManager", _writer.Object);

    private static JwtTokenManagerConfiguration Record(string? text)
        => new() { Id = ThingId, Name = "Jwt", Description = text };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderObservesTheWriteAndTheRemovalButNeverTheRead()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<ITokenManagerImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<ITokenManagerImplementationConfiguration>>();
        provider.ShouldNotBeAssignableTo<IConfigurationReadObserver<ITokenManagerImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheMemberHasNoInitializerAndNoReadOrSaveSelectsTheColumn()
    {
        new JwtTokenManagerConfiguration().Description.ShouldBeNull();
        new JwtTokenManagerConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveThatStatesNoTextWritesNothingSoTheStoredTextIsKept()
    {
        var stored = new Dictionary<Guid, string> { [ThingId] = "The stored text" };
        // The writer behaves as the real one does when it is called: a blank or missing text removes the stored one, a text replaces it. So a save that wrongly passed its null text on
        // would take "The stored text" away and fail the assertion below.
        _writer
            .Setup(w => w.Describe("TokenManager", "Text", ThingId, "Jwt", It.IsAny<string?>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, Guid, string, string?, ConfigurationWriteScope, CancellationToken>((_, _, id, _, text, _, _) =>
            {
                if (string.IsNullOrWhiteSpace(text)) stored.Remove(id); else stored[id] = text;
            })
            .ReturnsAsync(GenericResult.Success());
        var loaded = Record(null);

        var written = await Provider().Written(loaded, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        stored[ThingId].ShouldBe("The stored text");
        _writer.Verify(w => w.Describe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("A new text")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ASaveThatStatesATextDescribesTheThingAndABlankOneRemovesIt(string text)
    {
        _writer
            .Setup(w => w.Describe("TokenManager", "Text", ThingId, "Jwt", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("TokenManager", "Text", ThingId, "Jwt", "Text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record("Text"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARemovedThingTakesItsDescriptionsWithItInEveryLanguage()
    {
        _writer
            .Setup(w => w.DeleteAbout("TokenManager", ThingId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(ThingId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }
}
