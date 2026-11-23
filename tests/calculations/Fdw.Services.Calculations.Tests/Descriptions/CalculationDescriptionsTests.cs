using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests.Descriptions;

/// <summary>
/// A calculation's and a calculation version's description live in the descriptions table and not on <c>calc.CalculationEntity</c> or
/// <c>calc.CalculationVersion</c> (FDW-6208): each provider describes the thing in the unit of its save, fills the text once for every set of composed records
/// and once for every page of domain records it reads, and removes it with the thing. The writer and reader are stand-ins; <c>DescriptionServiceTests</c>
/// covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class CalculationDescriptionsTests
{
    private static readonly Guid Margin = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Growth = Guid.Parse("00000000-0000-0000-0000-0000000000a2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private CalculationConfigurationProvider Calculations()
        => new(
            NullLogger<CalculationConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "calc", "CalculationEntity",
            new UnavailablePlacement(), _writer.Object, _reader.Object);

    private CalculationVersionConfigurationProvider Versions()
        => new(
            NullLogger<CalculationVersionConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "calc", "CalculationVersion",
            _writer.Object, _reader.Object);

    // The reader answers with the texts it holds, put on each thing through the provider's own Id and fill delegates.
    private void TheReaderHolds<T>(string type, Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill(type, "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<T>, Func<T, Guid>, Action<T, string>, CancellationToken>(
                (_, _, things, idOf, fill, _) =>
                {
                    foreach (var thing in things)
                    {
                        if (texts.TryGetValue(idOf(thing), out var text)) fill(thing, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    private void TheReaderFails<T>(string type)
        => _reader
            .Setup(r => r.Fill(type, "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void EachProviderDeclaresTheWriteReadAndPageObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var calculations = Calculations();
        calculations.ShouldBeAssignableTo<IConfigurationWriteObserver<ICalculationEntityImplementationConfiguration>>();
        calculations.ShouldBeAssignableTo<IConfigurationReadObserver<ICalculationEntityImplementationConfiguration>>();
        calculations.ShouldBeAssignableTo<IConfigurationDomainReadObserver<CalculationEntityDomainConfiguration>>();
        calculations.ShouldBeAssignableTo<IReferencedConfigurationProvider<ICalculationEntityImplementationConfiguration>>();

        var versions = Versions();
        versions.ShouldBeAssignableTo<IConfigurationWriteObserver<ICalculationVersionImplementationConfiguration>>();
        versions.ShouldBeAssignableTo<IConfigurationReadObserver<ICalculationVersionImplementationConfiguration>>();
        versions.ShouldBeAssignableTo<IConfigurationDomainReadObserver<CalculationVersionDomainConfiguration>>();
        versions.ShouldBeAssignableTo<IReferencedConfigurationProvider<ICalculationVersionImplementationConfiguration>>();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(nameof(CalculationEntityDomainConfiguration))]
    [InlineData(nameof(CalculationVersionDomainConfiguration))]
    public void TheDomainRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt(string record)
    {
        var mapper = PocoMapperCollection.ByName(record);

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Revenue less cost, over revenue")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedCalculationIsDescribedUnderTheCalculationTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Calculation", "Text", Margin, "Margin", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Calculations().Written(new WindowedCalculationConfiguration { Id = Margin, Name = "Margin", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedVersionIsDescribedUnderTheCalculationVersionTypeInThePlacesWords()
    {
        _writer
            .Setup(w => w.Describe("CalculationVersion", "Text", Growth, "Margin", "Margin as the north region reports it", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Versions().Written(
            new CalculationVersionImplementationConfiguration { Id = Growth, Name = "Margin", Description = "Margin as the north region reports it" }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Calculation", "Text", Margin, "Margin", "Revenue less cost, over revenue", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Calculations().Written(
            new WindowedCalculationConfiguration { Id = Margin, Name = "Margin", Description = "Revenue less cost, over revenue" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedCalculationAndOneWithNoneStaysEmpty()
    {
        var calculations = new List<ICalculationEntityImplementationConfiguration>
        {
            new WindowedCalculationConfiguration { Id = Margin, Name = "Margin" },
            new WindowedCalculationConfiguration { Id = Growth, Name = "Growth" },
        };
        TheReaderHolds<ICalculationEntityImplementationConfiguration>("Calculation", new Dictionary<Guid, string> { [Margin] = "Revenue less cost, over revenue" });

        var read = await Calculations().Read(calculations, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        calculations[0].Description.ShouldBe("Revenue less cost, over revenue");
        calculations[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryCalculationOfAListPage()
    {
        var page = new List<CalculationEntityDomainConfiguration> { new() { Id = Margin, Name = "Margin" }, new() { Id = Growth, Name = "Growth" } };
        TheReaderHolds<CalculationEntityDomainConfiguration>("Calculation", new Dictionary<Guid, string> { [Growth] = "This year over last" });

        var read = await Calculations().Read(page, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        page[0].Description.ShouldBeNull();
        page[1].Description.ShouldBe("This year over last");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryVersionOfAPageAndOnEveryComposedVersion()
    {
        var page = new List<CalculationVersionDomainConfiguration> { new() { Id = Growth, Name = "Margin" } };
        var composed = new List<ICalculationVersionImplementationConfiguration> { new CalculationVersionImplementationConfiguration { Id = Growth, Name = "Margin" } };
        TheReaderHolds<CalculationVersionDomainConfiguration>("CalculationVersion", new Dictionary<Guid, string> { [Growth] = "Margin as the north region reports it" });
        TheReaderHolds<ICalculationVersionImplementationConfiguration>("CalculationVersion", new Dictionary<Guid, string> { [Growth] = "Margin as the north region reports it" });

        (await Versions().Read(page, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await Versions().Read(composed, null, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        page[0].Description.ShouldBe("Margin as the north region reports it");
        composed[0].Description.ShouldBe("Margin as the north region reports it");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfAPageAndOfComposedRecords()
    {
        TheReaderFails<CalculationEntityDomainConfiguration>("Calculation");
        TheReaderFails<ICalculationVersionImplementationConfiguration>("CalculationVersion");

        var page = await Calculations().Read([new CalculationEntityDomainConfiguration { Id = Margin, Name = "Margin" }], TestContext.Current.CancellationToken);
        var composed = await Versions().Read([new CalculationVersionImplementationConfiguration { Id = Growth, Name = "Margin" }], null, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        composed.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachReadPassesTheCallersTokenToTheFill()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill(
                "Calculation", "Text", It.IsAny<IReadOnlyList<ICalculationEntityImplementationConfiguration>>(), It.IsAny<Func<ICalculationEntityImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ICalculationEntityImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.Fill(
                "Calculation", "Text", It.IsAny<IReadOnlyList<CalculationEntityDomainConfiguration>>(), It.IsAny<Func<CalculationEntityDomainConfiguration, Guid>>(),
                It.IsAny<Action<CalculationEntityDomainConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Calculations().Read([new WindowedCalculationConfiguration { Id = Margin, Name = "Margin" }], null, cancelled.Token);
        await Calculations().Read([new CalculationEntityDomainConfiguration { Id = Margin, Name = "Margin" }], cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingACalculationOrAVersionRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Calculation", Margin, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteAbout("CalculationVersion", Growth, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Calculations().RemoveReferences(Margin, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await Versions().RemoveReferences(Growth, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
