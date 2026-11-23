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
/// A permission's description lives in the descriptions table and not in a column (FDW-6208): the provider describes the permission in the unit of its save, fills the text once for every set of
/// composed permissions it reads, fills a page of domain records once (the lists and the queries read pages of them) and removes it with the permission. The writer and reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class PermissionDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000000e1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000000e2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private PermissionConfigurationProvider Provider()
        => new(
            NullLogger<PermissionConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "authz", "Permission",
            _writer.Object, _reader.Object);

    private void TheReaderHolds<T>(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill("Permission", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
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
            .Setup(r => r.Fill("Permission", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IPermissionImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IPermissionImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IPermissionImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationDomainReadObserver<PermissionDomainConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        new PermissionDomainConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("What the permission is for.")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedPermissionIsDescribedUnderItsTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Permission", "Text", First, "first", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(new PermissionImplementationConfiguration { Id = First, Name = "first", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Permission", "Text", First, "first", "Text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(new PermissionImplementationConfiguration { Id = First, Name = "first", Description = "Text" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedPermissionAndOneWithNoneStaysEmpty()
    {
        var things = new List<IPermissionImplementationConfiguration>
        {
            new PermissionImplementationConfiguration { Id = First, Name = "first" },
            new PermissionImplementationConfiguration { Id = Second, Name = "second" },
        };
        TheReaderHolds<IPermissionImplementationConfiguration>(new Dictionary<Guid, string> { [First] = "The first one" });

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("The first one");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryDomainRecordOfAPage()
    {
        var page = new List<PermissionDomainConfiguration> { new() { Id = First, Name = "first" }, new() { Id = Second, Name = "second" } };
        TheReaderHolds<PermissionDomainConfiguration>(new Dictionary<Guid, string> { [Second] = "The second one" });

        var read = await Provider().Read(page, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        page[0].Description.ShouldBeNull();
        page[1].Description.ShouldBe("The second one");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfAPage()
    {
        TheReaderFails<PermissionDomainConfiguration>();

        var read = await Provider().Read([new PermissionDomainConfiguration { Id = First, Name = "first" }], TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheRead()
    {
        TheReaderFails<IPermissionImplementationConfiguration>();

        var read = await Provider().Read([new PermissionImplementationConfiguration { Id = First, Name = "first" }], null, TestContext.Current.CancellationToken);

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
            .Setup(r => r.Fill("Permission", "Text", It.IsAny<IReadOnlyList<IPermissionImplementationConfiguration>>(), It.IsAny<Func<IPermissionImplementationConfiguration, Guid>>(), It.IsAny<Action<IPermissionImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.Describe("Permission", "Text", First, "first", "Text", It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteAbout("Permission", First, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.Fill("Permission", "Text", It.IsAny<IReadOnlyList<PermissionDomainConfiguration>>(), It.IsAny<Func<PermissionDomainConfiguration, Guid>>(), It.IsAny<Action<PermissionDomainConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([new PermissionImplementationConfiguration { Id = First, Name = "first" }], null, cancelled.Token);
        await Provider().Written(new PermissionImplementationConfiguration { Id = First, Name = "first", Description = "Text" }, null!, cancelled.Token);
        await Provider().RemoveReferences(First, null!, cancelled.Token);
        await Provider().Read([new PermissionDomainConfiguration { Id = First, Name = "first" }], cancelled.Token);

        _reader.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingPermissionRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Permission", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
