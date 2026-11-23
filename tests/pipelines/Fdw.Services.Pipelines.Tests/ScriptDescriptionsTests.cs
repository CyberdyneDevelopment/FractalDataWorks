using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// A stored script's description lives in the descriptions table and not on <c>pipe.SqlScriptConfiguration</c> (FDW-7323, a group of FDW-6208): the provider describes the member in the unit of its
/// save, fills the text once for every set of composed members it reads, and removes it with the member. The text is kept under the Id of the domain row. The writer and reader
/// are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class ScriptDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-000000007351");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-000000007352");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private ScriptConfigurationProvider Provider()
        => new(
            NullLogger<ScriptConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "pipe", "Script",
            _writer.Object, _reader.Object);

    private static SqlScriptConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<ISqlScriptImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<ISqlScriptImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<ISqlScriptImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheImplementationsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(SqlScriptConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Loads the nightly extract into staging")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedMemberIsDescribedUnderTheTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("SqlScript", "Text", First, "NightlyLoad", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "NightlyLoad", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("SqlScript", "Text", First, "NightlyLoad", "Loads the nightly extract into staging", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "NightlyLoad", "Loads the nightly extract into staging"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedMemberAndOneWithNoneStaysEmpty()
    {
        var things = new List<ISqlScriptImplementationConfiguration> { Record(First, "NightlyLoad"), Record(Second, "other") };
        _reader
            .Setup(r => r.Fill("SqlScript", "Text", It.IsAny<IReadOnlyList<ISqlScriptImplementationConfiguration>>(), It.IsAny<Func<ISqlScriptImplementationConfiguration, Guid>>(), It.IsAny<Action<ISqlScriptImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<ISqlScriptImplementationConfiguration>, Func<ISqlScriptImplementationConfiguration, Guid>, Action<ISqlScriptImplementationConfiguration, string>, CancellationToken>(
                (_, _, found, idOf, fill, _) =>
                {
                    foreach (var thing in found)
                    {
                        if (idOf(thing) == First) fill(thing, "Loads the nightly extract into staging");
                    }
                })
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Loads the nightly extract into staging");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfComposedMembers()
    {
        _reader
            .Setup(r => r.Fill("SqlScript", "Text", It.IsAny<IReadOnlyList<ISqlScriptImplementationConfiguration>>(), It.IsAny<Func<ISqlScriptImplementationConfiguration, Guid>>(), It.IsAny<Action<ISqlScriptImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Record(First, "NightlyLoad")], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAMemberRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("SqlScript", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
