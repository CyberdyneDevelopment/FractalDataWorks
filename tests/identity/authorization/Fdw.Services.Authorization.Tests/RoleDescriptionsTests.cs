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
/// A role's description lives in the descriptions table and not in a column (FDW-6208): the provider describes the role in the unit of its save, fills the text once for every set of
/// composed roles it reads, fills a page of domain records once (the lists and the queries read pages of them) and removes it with the role. The writer and reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class RoleDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000000e1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000000e2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);
    private readonly Mock<IUserRoleConfigurationProvider> _userRoles = new(MockBehavior.Strict);
    private readonly Mock<IRolePermissionConfigurationProvider> _rolePermissions = new(MockBehavior.Strict);

    private RoleConfigurationProvider Provider()
        => new(
            NullLogger<RoleConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "authz", "Role",
            _userRoles.Object, _rolePermissions.Object,
            _writer.Object, _reader.Object);

    private void TheReaderHolds<T>(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill("Role", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
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
            .Setup(r => r.Fill("Role", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IRoleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IRoleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IRoleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationDomainReadObserver<RoleDomainConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        new RoleDomainConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("What the role is for.")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedRoleIsDescribedUnderItsTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Role", "Text", First, "first", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(new RoleImplementationConfiguration { Id = First, Name = "first", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Role", "Text", First, "first", "Text", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(new RoleImplementationConfiguration { Id = First, Name = "first", Description = "Text" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedRoleAndOneWithNoneStaysEmpty()
    {
        var things = new List<IRoleImplementationConfiguration>
        {
            new RoleImplementationConfiguration { Id = First, Name = "first" },
            new RoleImplementationConfiguration { Id = Second, Name = "second" },
        };
        TheReaderHolds<IRoleImplementationConfiguration>(new Dictionary<Guid, string> { [First] = "The first one" });

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("The first one");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryDomainRecordOfAPage()
    {
        var page = new List<RoleDomainConfiguration> { new() { Id = First, Name = "first" }, new() { Id = Second, Name = "second" } };
        TheReaderHolds<RoleDomainConfiguration>(new Dictionary<Guid, string> { [Second] = "The second one" });

        var read = await Provider().Read(page, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        page[0].Description.ShouldBeNull();
        page[1].Description.ShouldBe("The second one");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfAPage()
    {
        TheReaderFails<RoleDomainConfiguration>();

        var read = await Provider().Read([new RoleDomainConfiguration { Id = First, Name = "first" }], TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheRead()
    {
        TheReaderFails<IRoleImplementationConfiguration>();

        var read = await Provider().Read([new RoleImplementationConfiguration { Id = First, Name = "first" }], null, TestContext.Current.CancellationToken);

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
            .Setup(r => r.Fill("Role", "Text", It.IsAny<IReadOnlyList<IRoleImplementationConfiguration>>(), It.IsAny<Func<IRoleImplementationConfiguration, Guid>>(), It.IsAny<Action<IRoleImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.Describe("Role", "Text", First, "first", "Text", It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteAbout("Role", First, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _userRoles.Setup(u => u.RemoveAssignmentsOf(First, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult<int>.Success(0));
        _rolePermissions.Setup(p => p.RemoveGrantsOf(First, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult<int>.Success(0));
        _reader
            .Setup(r => r.Fill("Role", "Text", It.IsAny<IReadOnlyList<RoleDomainConfiguration>>(), It.IsAny<Func<RoleDomainConfiguration, Guid>>(), It.IsAny<Action<RoleDomainConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([new RoleImplementationConfiguration { Id = First, Name = "first" }], null, cancelled.Token);
        await Provider().Written(new RoleImplementationConfiguration { Id = First, Name = "first", Description = "Text" }, null!, cancelled.Token);
        await Provider().RemoveReferences(First, null!, cancelled.Token);
        await Provider().Read([new RoleDomainConfiguration { Id = First, Name = "first" }], cancelled.Token);

        _reader.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingRoleRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Role", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        _userRoles.Setup(u => u.RemoveAssignmentsOf(First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<int>.Success(0));
        _rolePermissions.Setup(p => p.RemoveGrantsOf(First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<int>.Success(0));

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
