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
using Fdw.Services.Pipelines.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// A webhook trigger's description lives in the descriptions table and not on <c>pipe.WebhookTriggerConfiguration</c> (FDW-7323, a group of FDW-6208): the provider describes the member in the unit of its
/// save, fills the text once for every set of composed members it reads, and removes it with the member. The text is kept under the Id of the domain row. The writer and reader
/// are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class WebhookTriggerDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-000000007361");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-000000007362");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private WebhookTriggerConfigurationProvider Provider()
        => new(
            NullLogger<WebhookTriggerConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "pipe", "WebhookTrigger",
            _writer.Object, _reader.Object);

    private static WebhookTriggerConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IWebhookTriggerImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IWebhookTriggerImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IWebhookTriggerImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheImplementationsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(WebhookTriggerConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Starts the consolidation when an order ships")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedMemberIsDescribedUnderTheTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("WebhookTrigger", "Text", First, "OrderShipped", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "OrderShipped", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("WebhookTrigger", "Text", First, "OrderShipped", "Starts the consolidation when an order ships", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "OrderShipped", "Starts the consolidation when an order ships"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedMemberAndOneWithNoneStaysEmpty()
    {
        var things = new List<IWebhookTriggerImplementationConfiguration> { Record(First, "OrderShipped"), Record(Second, "other") };
        _reader
            .Setup(r => r.Fill("WebhookTrigger", "Text", It.IsAny<IReadOnlyList<IWebhookTriggerImplementationConfiguration>>(), It.IsAny<Func<IWebhookTriggerImplementationConfiguration, Guid>>(), It.IsAny<Action<IWebhookTriggerImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IWebhookTriggerImplementationConfiguration>, Func<IWebhookTriggerImplementationConfiguration, Guid>, Action<IWebhookTriggerImplementationConfiguration, string>, CancellationToken>(
                (_, _, found, idOf, fill, _) =>
                {
                    foreach (var thing in found)
                    {
                        if (idOf(thing) == First) fill(thing, "Starts the consolidation when an order ships");
                    }
                })
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Starts the consolidation when an order ships");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfComposedMembers()
    {
        _reader
            .Setup(r => r.Fill("WebhookTrigger", "Text", It.IsAny<IReadOnlyList<IWebhookTriggerImplementationConfiguration>>(), It.IsAny<Func<IWebhookTriggerImplementationConfiguration, Guid>>(), It.IsAny<Action<IWebhookTriggerImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Record(First, "OrderShipped")], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAMemberRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("WebhookTrigger", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
