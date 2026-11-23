using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.ClientCredentials;
using Fdw.Services.Identity.JwtAssertion;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Identity.Tests;

/// <summary>
/// An identity's description lives in the descriptions table, never in an identity table (FDW-6208), and this domain never fills a record on load: the provider writes a text in the
/// unit of the save (a save that states none keeps the stored one, a stated blank removes it) and removes it with the identity. The writer is a strict stand-in, so any call a case does
/// not expect is a failure.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class IdentityDescriptionsTests
{
    private static readonly Guid IdentityId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);

    private IdentityServiceConfigurationProvider Provider()
        => new(NullLogger<IdentityServiceConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "sec", "Identity", _writer.Object);

    private void ExpectDescribe(string text)
        => _writer
            .Setup(w => w.Describe("Identity", "Text", IdentityId, "scheduler", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

    public static TheoryData<IIdentityServiceImplementationConfiguration> Bodies()
        => new()
        {
            new ClientCredentialsConfiguration { Id = IdentityId, Name = "scheduler" },
            new JwtAssertionConfiguration { Id = IdentityId, Name = "scheduler" },
        };

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(typeof(ClientCredentialsConfiguration))]
    [InlineData(typeof(JwtAssertionConfiguration))]
    public void EachBodysDescriptionIsANullableStringWithNoInitializerAndIsNotStored(Type body)
    {
        var member = body.GetProperty("Description").ShouldNotBeNull();

        member.GetCustomAttribute<NotStoredAttribute>().ShouldNotBeNull();
        member.PropertyType.ShouldBe(typeof(string));
        new NullabilityInfoContext().Create(member).WriteState.ShouldBe(NullabilityState.Nullable);
        member.GetValue(Activator.CreateInstance(body)).ShouldBeNull();
        typeof(IDescribedConfiguration).IsAssignableFrom(body).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderIsAWriteObserverAndAReferenceProviderAndNeverAReadObserver()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IIdentityServiceImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IIdentityServiceImplementationConfiguration>>();
        provider.ShouldNotBeAssignableTo<IConfigurationReadObserver<IIdentityServiceImplementationConfiguration>>();
        provider.ShouldNotBeAssignableTo<IConfigurationChildReadObserver<IIdentityServiceImplementationConfiguration>>();
    }

    [Theory]
    [MemberData(nameof(Bodies))]
    [Trait("Priority", "P0")]
    public async Task ATextTheSaveStatesIsWrittenInTheUnitOfTheSave(IIdentityServiceImplementationConfiguration body)
    {
        ExpectDescribe("The scheduler's service identity");
        body.Description = "The scheduler's service identity";

        var written = await Provider().Written(body, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(Bodies))]
    [Trait("Priority", "P0")]
    public async Task AStatedBlankRemovesTheStoredText(IIdentityServiceImplementationConfiguration body)
    {
        // The writer's rule: empty or blank text removes the description; the provider passes the stated blank on and does not decide it.
        ExpectDescribe(string.Empty);
        body.Description = string.Empty;

        var written = await Provider().Written(body, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(Bodies))]
    [Trait("Priority", "P0")]
    public async Task ARecordReadWithItsTextStoredAndSavedUnchangedKeepsTheTextBecauseNothingIsWritten(IIdentityServiceImplementationConfiguration read)
    {
        // A record that was read carries no text (this domain never fills on load): saving it unchanged states none, so the writer is not called at all (strict: any call fails).
        read.Description.ShouldBeNull();

        var written = await Provider().Written(read, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedWriteFailsTheSave()
    {
        _writer
            .Setup(w => w.Describe("Identity", "Text", IdentityId, "scheduler", "a text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(new ClientCredentialsConfiguration { Id = IdentityId, Name = "scheduler", Description = "a text" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAnIdentityRemovesItsDescriptionInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Identity", IdentityId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(IdentityId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        _writer.VerifyAll();
    }
}
