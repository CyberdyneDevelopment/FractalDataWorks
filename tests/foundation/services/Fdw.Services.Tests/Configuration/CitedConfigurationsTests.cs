using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// FDW-6221: a record that cites another domain's member asks <see cref="ICitedConfigurations"/> whether it exists. The domain is found by the
/// container its provider names, the answer is the provider's own, and a citation that declares it needs no check is not asked at all.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataIntegrity")]
public sealed class CitedConfigurationsTests
{
    private static Mock<IDomainConfigurationProvider> Provider(string container, ICitedState state)
    {
        var provider = new Mock<IDomainConfigurationProvider>(MockBehavior.Strict);
        provider.SetupGet(p => p.Container).Returns(container);
        provider
            .Setup(p => p.StateOf(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICitedState>.Success(state));
        return provider;
    }

    private static CitedConfigurations Resolver(params IDomainConfigurationProvider[] providers) =>
        new(providers, NullLogger<CitedConfigurations>.Instance);

    [Fact]
    public async Task ACitedMemberThatExistsIsFound()
    {
        var id = Guid.NewGuid();
        var data = Provider("data.DataSet", state: CitedStates.Current);

        var found = await Resolver(data.Object).StateOf(Citation.Of("data.DataSet"), id, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.ShouldBe(CitedStates.Current);
        data.Verify(p => p.StateOf(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ARetiredCitedMemberIsReportedRetiredAndNotAsAFailure()
    {
        var found = await Resolver(Provider("data.DataSet", state: CitedStates.Retired).Object)
            .StateOf(Citation.Of("data.DataSet"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.ShouldBe(CitedStates.Retired);
    }

    [Fact]
    public async Task ACitedMemberThatDoesNotExistIsReportedAbsentAndNotAsAFailure()
    {
        var found = await Resolver(Provider("data.DataSet", state: CitedStates.Absent).Object)
            .StateOf(Citation.Of("data.DataSet"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.ShouldBe(CitedStates.Absent);
    }

    [Fact]
    public async Task TheProviderOfTheNamedContainerIsTheOneAsked()
    {
        var data = Provider("data.DataSet", state: CitedStates.Current);
        var stores = Provider("data.DataStore", state: CitedStates.Absent);

        var found = await Resolver(stores.Object, data.Object).StateOf(Citation.Of("data.DataSet"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.Value.ShouldBe(CitedStates.Current);
        stores.Verify(p => p.StateOf(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AContainerNoProviderIsRegisteredForFailsAndNamesItAndWhatIsRegistered()
    {
        var found = await Resolver(Provider("data.DataSet", state: CitedStates.Current).Object)
            .StateOf(Citation.Of("conn.Connection"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Code!.Name.ShouldBe("CitedContainerNotRegistered");
        found.Details!.GetValue<string>("container").ShouldBe("conn.Connection");
        found.Details!.GetValue<string>("registered").ShouldBe("data.DataSet");
    }

    [Fact]
    public async Task TwoProvidersThatNameOneContainerFail()
    {
        var found = await Resolver(Provider("data.DataSet", state: CitedStates.Current).Object, Provider("data.DataSet", state: CitedStates.Current).Object)
            .StateOf(Citation.Of("data.DataSet"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Code!.Name.ShouldBe("CitedContainerAmbiguous");
    }

    [Fact]
    public async Task AReadThatFailsFailsTheCitationToo()
    {
        var provider = new Mock<IDomainConfigurationProvider>(MockBehavior.Strict);
        provider.SetupGet(p => p.Container).Returns("data.DataSet");
        provider
            .Setup(p => p.StateOf(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICitedState>.Failure(new Fdw.Messages.GenericMessage("gateway exploded")));

        var found = await Resolver(provider.Object).StateOf(Citation.Of("data.DataSet"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ACitationTheDatabaseEnforcesIsNotAskedOfAnyProvider()
    {
        var data = Provider("data.DataSet", state: CitedStates.Absent);

        var found = await Resolver(data.Object)
            .StateOf(Citation.EnforcedByDatabase("FK_PipelineThreshold_Pipeline"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.ShouldBe(CitedStates.Unchecked);
        data.Verify(p => p.StateOf(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ACitationThatCitesNothingIsNotAskedOfAnyProvider()
    {
        var data = Provider("data.DataSet", state: CitedStates.Absent);

        var found = await Resolver(data.Object)
            .StateOf(Citation.Uncited("a server is not a configuration row"), Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.Value.ShouldBe(CitedStates.Unchecked);
        data.Verify(p => p.StateOf(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ACitationStatesWhatItCitesOrWhyItNeedsNoCheck()
    {
        Citation.Of("data.DataSet").Container.ShouldBe("data.DataSet");
        Citation.EnforcedByDatabase("FK_X").Container.ShouldBeNull();
        Should.Throw<ArgumentException>(() => Citation.Of(" "));
        Should.Throw<ArgumentException>(() => Citation.EnforcedByDatabase(string.Empty));
        Should.Throw<ArgumentException>(() => Citation.Uncited(string.Empty));
    }

    [Fact]
    public async Task ACheckWhoseTokenIsAlreadyCancelledEndsWithTheCodedCancelledResultAndAsksNoProvider()
    {
        var data = Provider("data.DataSet", state: CitedStates.Current);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var found = await Resolver(data.Object).StateOf(Citation.Of("data.DataSet"), Guid.NewGuid(), source.Token);

        found.IsFailure.ShouldBeTrue();
        found.Messages[^1].Code.ShouldBe("SERVICES-10010");
        data.Verify(p => p.StateOf(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ACheckCancelledWhileTheProviderReadsEndsWithTheCodedCancelledResult()
    {
        using var source = new CancellationTokenSource();
        var provider = new Mock<IDomainConfigurationProvider>(MockBehavior.Strict);
        provider.SetupGet(p => p.Container).Returns("data.DataSet");
        provider
            .Setup(p => p.StateOf(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid _, CancellationToken token) =>
            {
                await source.CancelAsync();
                token.ThrowIfCancellationRequested();
                return GenericResult<ICitedState>.Success(CitedStates.Current);
            });

        var found = await Resolver(provider.Object).StateOf(Citation.Of("data.DataSet"), Guid.NewGuid(), source.Token);

        found.IsFailure.ShouldBeTrue();
        found.Messages[^1].Code.ShouldBe("SERVICES-10010");
    }

    [Fact]
    public async Task ACitationNobodyIsAskedAboutIsNotTouchedByACancelledToken()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var found = await Resolver().StateOf(Citation.Uncited("a server is not a configuration row"), Guid.NewGuid(), source.Token);

        found.IsSuccess.ShouldBeTrue();
        found.Value.ShouldBe(CitedStates.Unchecked);
    }
}
