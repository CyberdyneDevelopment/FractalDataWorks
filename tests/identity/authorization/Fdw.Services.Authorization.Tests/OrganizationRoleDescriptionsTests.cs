using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// A organization role's description lives in the descriptions table and not in a column (FDW-6208): the provider describes the organization role in the unit of its save, fills the text once for every set of
/// composed organization roles it reads and removes it with the organization role. The writer and reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class OrganizationRoleDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000000e1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000000e2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private OrganizationRoleConfigurationProvider Provider()
        => new(
            NullLogger<OrganizationRoleConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "tenant", "OrganizationRole",
            _writer.Object, _reader.Object);

    private void TheReaderHolds<T>(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill("OrganizationRole", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<T>, Func<T, Guid>, Action<T, string>, CancellationToken>(
                (_, _, things, idOf, fill, _) =>
                {
                    foreach (var thing in things)
                    {
                        if (texts.TryGetValue(idOf(thing), out var text)) fill(thing, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    private void TheReaderFails<T>()
        => _reader
            .Setup(r => r.Fill("OrganizationRole", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IOrganizationRoleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IOrganizationRoleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IOrganizationRoleImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        new OrganizationRoleImplementationConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("What the organization role is for.")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedOrganizationRoleIsDescribedUnderItsTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("OrganizationRole", "Text", First, "first", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(new OrganizationRoleImplementationConfiguration { Id = First, Name = "first", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("OrganizationRole", "Text", First, "first", "Text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(new OrganizationRoleImplementationConfiguration { Id = First, Name = "first", Description = "Text" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedOrganizationRoleAndOneWithNoneStaysEmpty()
    {
        var things = new List<IOrganizationRoleImplementationConfiguration>
        {
            new OrganizationRoleImplementationConfiguration { Id = First, Name = "first" },
            new OrganizationRoleImplementationConfiguration { Id = Second, Name = "second" },
        };
        TheReaderHolds<IOrganizationRoleImplementationConfiguration>(new Dictionary<Guid, string> { [First] = "The first one" });

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("The first one");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheRead()
    {
        TheReaderFails<IOrganizationRoleImplementationConfiguration>();

        var read = await Provider().Read([new OrganizationRoleImplementationConfiguration { Id = First, Name = "first" }], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachReadAndWritePassesTheCallersToken()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill("OrganizationRole", "Text", It.IsAny<IReadOnlyList<IOrganizationRoleImplementationConfiguration>>(), It.IsAny<Func<IOrganizationRoleImplementationConfiguration, Guid>>(), It.IsAny<Action<IOrganizationRoleImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.Describe("OrganizationRole", "Text", First, "first", "Text", It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteAbout("OrganizationRole", First, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());

        await Provider().Read([new OrganizationRoleImplementationConfiguration { Id = First, Name = "first" }], null, cancelled.Token);
        await Provider().Written(new OrganizationRoleImplementationConfiguration { Id = First, Name = "first", Description = "Text" }, null!, cancelled.Token);
        await Provider().RemoveReferences(First, null!, cancelled.Token);

        _reader.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingOrganizationRoleRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("OrganizationRole", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
