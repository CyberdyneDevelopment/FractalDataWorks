using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A category's description lives in the descriptions table and not on <c>catalog.Category</c> (FDW-6815): the provider describes the category in the unit of its
/// save, fills the text once for every set of composed categories and once for every page of domain records it reads (the category tree), and removes it with the
/// category. The writer and reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class CategoryDescriptionsTests
{
    private static readonly Guid Sports = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid Nfl = Guid.Parse("00000000-0000-0000-0000-0000000000c2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private CategoryConfigurationProvider Categories()
        => new(
            NullLogger<CategoryConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "catalog", "Category",
            _writer.Object, _reader.Object);

    // The reader answers with the texts it holds, put on each category through the provider's own Id and fill delegates.
    private void TheReaderHolds<T>(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill("CatalogCategory", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
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
            .Setup(r => r.Fill("CatalogCategory", "Text", It.IsAny<IReadOnlyList<T>>(), It.IsAny<Func<T, Guid>>(), It.IsAny<Action<T, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteReadAndPageObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var categories = Categories();

        categories.ShouldBeAssignableTo<IConfigurationWriteObserver<ICategoryImplementationConfiguration>>();
        categories.ShouldBeAssignableTo<IConfigurationReadObserver<ICategoryImplementationConfiguration>>();
        categories.ShouldBeAssignableTo<IConfigurationDomainReadObserver<CategoryDomainConfiguration>>();
        categories.ShouldBeAssignableTo<IReferencedConfigurationProvider<ICategoryImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDomainRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        new CategoryDomainConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Data about sport: teams, games, players and the results.")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedCategoryIsDescribedUnderTheCategoryTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("CatalogCategory", "Text", Sports, "sports", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Categories().Written(
            new DataSetCategoryConfiguration { Id = Sports, Name = "sports", DisplayName = "Sports", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("CatalogCategory", "Text", Sports, "sports", "Sport", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Categories().Written(
            new CustomCategoryConfiguration { Id = Sports, Name = "sports", DisplayName = "Sports", Description = "Sport" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedCategoryAndOneWithNoneStaysEmpty()
    {
        var categories = new List<ICategoryImplementationConfiguration>
        {
            new DataSetCategoryConfiguration { Id = Sports, Name = "sports", DisplayName = "Sports" },
            new GlossaryCategoryConfiguration { Id = Nfl, Name = "nfl", DisplayName = "NFL" },
        };
        TheReaderHolds<ICategoryImplementationConfiguration>(new Dictionary<Guid, string> { [Sports] = "Data about sport" });

        var read = await Categories().Read(categories, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        categories[0].Description.ShouldBe("Data about sport");
        categories[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryCategoryOfTheTreesPage()
    {
        var page = new List<CategoryDomainConfiguration>
        {
            new() { Id = Sports, Name = "sports", DisplayName = "Sports" },
            new() { Id = Nfl, Name = "nfl", DisplayName = "NFL", ParentCategoryId = Sports },
        };
        TheReaderHolds<CategoryDomainConfiguration>(new Dictionary<Guid, string> { [Nfl] = "The National Football League." });

        var read = await Categories().Read(page, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        page[0].Description.ShouldBeNull();
        page[1].Description.ShouldBe("The National Football League.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfAPageAndOfComposedCategories()
    {
        TheReaderFails<CategoryDomainConfiguration>();
        TheReaderFails<ICategoryImplementationConfiguration>();

        var page = await Categories().Read([new CategoryDomainConfiguration { Id = Sports, Name = "sports" }], TestContext.Current.CancellationToken);
        var composed = await Categories().Read([new CustomCategoryConfiguration { Id = Nfl, Name = "nfl" }], null, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        composed.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachReadAndWritePassesTheCallersToken()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill(
                "CatalogCategory", "Text", It.IsAny<IReadOnlyList<ICategoryImplementationConfiguration>>(), It.IsAny<Func<ICategoryImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ICategoryImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.Fill(
                "CatalogCategory", "Text", It.IsAny<IReadOnlyList<CategoryDomainConfiguration>>(), It.IsAny<Func<CategoryDomainConfiguration, Guid>>(),
                It.IsAny<Action<CategoryDomainConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.Describe("CatalogCategory", "Text", Sports, "sports", "Sport", It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteAbout("CatalogCategory", Sports, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());

        await Categories().Read([new DataSetCategoryConfiguration { Id = Sports, Name = "sports" }], null, cancelled.Token);
        await Categories().Read([new CategoryDomainConfiguration { Id = Sports, Name = "sports" }], cancelled.Token);
        await Categories().Written(new DataSetCategoryConfiguration { Id = Sports, Name = "sports", Description = "Sport" }, null!, cancelled.Token);
        await Categories().RemoveReferences(Sports, null!, cancelled.Token);

        _reader.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingACategoryRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("CatalogCategory", Sports, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Categories().RemoveReferences(Sports, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
