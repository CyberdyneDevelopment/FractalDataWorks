using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Tags.Links;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tags.Tests;

/// <summary>The Tags service over stand-ins for the tag provider, the scope rule and the configuration gateway.</summary>
[Trait("Category", "Tags")]
public sealed class TagServiceTests
{
    private const string Store = "PlatformConfiguration";
    private static readonly Guid Thing = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid TagId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    private readonly Mock<ITagConfigurationProvider> _tags = new();
    private readonly Mock<ITagScope> _scope = new();
    private readonly Mock<IConfigurationGateway> _gateway = new();
    private readonly List<IDataCommand> _written = [];

    private TagService Service()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(Store)).Returns(GenericResult<IConfigurationGateway>.Success(_gateway.Object));
        _gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) => _written.Add(command))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null)));
        _scope
            .Setup(s => s.Allows(It.IsAny<TagDomainConfiguration>(), It.IsAny<ITaggableKind>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return new TagService(_tags.Object, gateways.Object, _scope.Object, Store, NullLogger<TagService>.Instance);
    }

    private void TagExists(string name, string ownerKind = "Tenant")
    {
        _tags
            .Setup(t => t.Get(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITagImplementationConfiguration>.Success(new TagImplementationConfiguration { Id = TagId, Name = name }));
        _tags
            .Setup(t => t.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TagDomainConfiguration>>.Success(
                [new TagDomainConfiguration { Id = TagId, Name = name, OwnerKind = ownerKind, OwnerId = Guid.NewGuid() }]));
    }

    private void LinksOfTheThing(params DataSetTagConfiguration[] links)
        => _gateway
            .Setup(g => g.Execute<IEnumerable<DataSetTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetTagConfiguration>>.Success(links));

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LinkInsertsOneRowWhenTheTagIsNotOnTheThingYet()
    {
        TagExists("finance");
        LinksOfTheThing();

        var linked = await Service().Link("DataSet", Thing, "finance", TestContext.Current.CancellationToken);

        linked.IsSuccess.ShouldBeTrue();
        _written.ShouldHaveSingleItem().ShouldBeOfType<ConfigurationSaveCommand<DataSetTagConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LinkRefusesATagTheThingAlreadyCarriesAndWritesNothing()
    {
        TagExists("finance");
        LinksOfTheThing(new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = Thing, TagId = TagId });

        var linked = await Service().Link("DataSet", Thing, "finance", TestContext.Current.CancellationToken);

        linked.IsFailure.ShouldBeTrue();
        _written.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LinkWritesNothingWhenTheScopeRefuses()
    {
        TagExists("finance", "Workspace");
        LinksOfTheThing();
        var service = Service();
        _scope
            .Setup(s => s.Allows(It.IsAny<TagDomainConfiguration>(), It.IsAny<ITaggableKind>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(TagsResultCodes.ByName("TagNotInScope"), NullLogger.Instance, ResultDetails.Create("name", "finance")));

        var linked = await service.Link("DataSet", Thing, "finance", TestContext.Current.CancellationToken);

        linked.IsFailure.ShouldBeTrue();
        _written.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LinkRefusesAnUnknownKind()
    {
        var linked = await Service().Link("Banana", Thing, "finance", TestContext.Current.CancellationToken);

        linked.IsFailure.ShouldBeTrue();
        _written.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task UnlinkRetiresTheLinkItFinds()
    {
        TagExists("finance");
        LinksOfTheThing(new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = Thing, TagId = TagId });

        var unlinked = await Service().Unlink("DataSet", Thing, "finance", TestContext.Current.CancellationToken);

        unlinked.IsSuccess.ShouldBeTrue();
        _written.ShouldHaveSingleItem().ShouldBeOfType<ConfigurationRetireCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task UnlinkFailsWhenTheThingDoesNotCarryTheTag()
    {
        TagExists("finance");
        LinksOfTheThing();

        var unlinked = await Service().Unlink("DataSet", Thing, "finance", TestContext.Current.CancellationToken);

        unlinked.IsFailure.ShouldBeTrue();
        _written.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P0")]
    public async Task CreateRefusesABlankName(string name)
    {
        var created = await Service().Create(name, "Tenant", Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        created.IsFailure.ShouldBeTrue();
        _tags.Verify(t => t.Save(It.IsAny<ITagImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task CreateRefusesANameLongerThanTheColumn()
    {
        var created = await Service().Create(new string('x', 101), "Tenant", Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        created.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task CreateRefusesANameAnyScopeAlreadyHolds()
    {
        TagExists("finance", "Organization");

        var created = await Service().Create("finance", "Workspace", Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        created.IsFailure.ShouldBeTrue();
        _tags.Verify(t => t.Save(It.IsAny<ITagImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeleteRetiresTheLinksOfEveryKindBeforeTheTag()
    {
        TagExists("finance");
        var service = Service();
        _gateway
            .Setup(g => g.Execute<IEnumerable<DataSetTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetTagConfiguration>>.Success([new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = Thing, TagId = TagId }]));
        _gateway
            .Setup(g => g.Execute<IEnumerable<PipelineTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<PipelineTagConfiguration>>.Success([]));
        _gateway
            .Setup(g => g.Execute<IEnumerable<DataStoreTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataStoreTagConfiguration>>.Success([]));
        _gateway
            .Setup(g => g.Execute<IEnumerable<ConnectionTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ConnectionTagConfiguration>>.Success([]));
        _gateway
            .Setup(g => g.Execute<IEnumerable<ScheduleTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleTagConfiguration>>.Success([]));
        _gateway
            .Setup(g => g.Execute<IEnumerable<CalculationTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<CalculationTagConfiguration>>.Success([]));
        _tags.Setup(t => t.Delete(TagId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var deleted = await service.Delete("finance", TestContext.Current.CancellationToken);

        deleted.IsSuccess.ShouldBeTrue();
        _written.ShouldHaveSingleItem().ShouldBeOfType<ConfigurationRetireCommand>();
        _tags.Verify(t => t.Delete(TagId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ThingsOfKeepsOnlyThingsCarryingEveryNamedTag()
    {
        TagExists("finance");
        var other = Guid.NewGuid();
        LinksOfTheThing(
            new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = Thing, TagId = TagId },
            new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = other, TagId = TagId });

        var things = await Service().ThingsOf("DataSet", ["finance"], TestContext.Current.CancellationToken);

        things.IsSuccess.ShouldBeTrue();
        things.Value.ShouldNotBeNull().OrderBy(id => id).ToArray().ShouldBe(new[] { Thing, other }.OrderBy(id => id).ToArray());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TagsByThingNamesTheTagsOfEveryThingInOneReadOfTheLinksAndOneOfTheTags()
    {
        var other = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        var second = Guid.Parse("00000000-0000-0000-0000-0000000000d2");
        _tags
            .Setup(t => t.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TagDomainConfiguration>>.Success(
            [
                new TagDomainConfiguration { Id = TagId, Name = "finance", OwnerKind = "Tenant", OwnerId = Guid.NewGuid() },
                new TagDomainConfiguration { Id = second, Name = "Audit", OwnerKind = "Tenant", OwnerId = Guid.NewGuid() },
            ]));
        LinksOfTheThing(
            new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = Thing, TagId = TagId },
            new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = Thing, TagId = second },
            new DataSetTagConfiguration { Id = Guid.NewGuid(), DataSetId = other, TagId = TagId });

        var byThing = await Service().TagsByThing("DataSet", TestContext.Current.CancellationToken);

        byThing.IsSuccess.ShouldBeTrue();
        byThing.Value![Thing].ShouldBe(["Audit", "finance"]);
        byThing.Value[other].ShouldBe(["finance"]);
        _gateway.Verify(g => g.Execute<IEnumerable<DataSetTagConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
        _tags.Verify(t => t.Get(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TagsByThingIsEmptyWhenNothingOfTheKindIsTagged()
    {
        LinksOfTheThing();

        var byThing = await Service().TagsByThing("DataSet", TestContext.Current.CancellationToken);

        byThing.IsSuccess.ShouldBeTrue();
        byThing.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TagsByThingRefusesAnUnknownKind()
    {
        var byThing = await Service().TagsByThing("Planet", TestContext.Current.CancellationToken);

        byThing.IsFailure.ShouldBeTrue();
    }
}
