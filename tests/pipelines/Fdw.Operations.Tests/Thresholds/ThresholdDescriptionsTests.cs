using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>
/// A threshold's description lives in the descriptions table and not on <c>workflow.ThresholdConfiguration</c> (FDW-7205, a group of FDW-6208): the provider describes the threshold in the unit of its save,
/// fills the text once for every set of composed thresholds it reads, and removes it with the threshold. The text is kept under the Id of the domain row. The writer and reader
/// are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class ThresholdDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000007e1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000007e2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private ThresholdConfigurationProvider Provider()
        => new(
            NullLogger<ThresholdConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "workflow", "Threshold",
            _writer.Object, _reader.Object);

    private static ThresholdImplementationConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IThresholdImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IThresholdImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IThresholdImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheImplementationsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(ThresholdImplementationConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Pauses a schedule when three of its fires in a row failed")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedThresholdIsDescribedUnderTheTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Threshold", "Text", First, "ScheduleFailureLimit", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "ScheduleFailureLimit", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Threshold", "Text", First, "ScheduleFailureLimit", "Pauses a schedule when three of its fires in a row failed", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "ScheduleFailureLimit", "Pauses a schedule when three of its fires in a row failed"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedThresholdAndOneWithNoneStaysEmpty()
    {
        var things = new List<IThresholdImplementationConfiguration> { Record(First, "ScheduleFailureLimit"), Record(Second, "other") };
        _reader
            .Setup(r => r.Fill("Threshold", "Text", It.IsAny<IReadOnlyList<IThresholdImplementationConfiguration>>(), It.IsAny<Func<IThresholdImplementationConfiguration, Guid>>(), It.IsAny<Action<IThresholdImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IThresholdImplementationConfiguration>, Func<IThresholdImplementationConfiguration, Guid>, Action<IThresholdImplementationConfiguration, string>, CancellationToken>(
                (_, _, found, idOf, fill, _) =>
                {
                    foreach (var thing in found)
                    {
                        if (idOf(thing) == First) fill(thing, "Pauses a schedule when three of its fires in a row failed");
                    }
                })
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Pauses a schedule when three of its fires in a row failed");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfComposedThresholds()
    {
        _reader
            .Setup(r => r.Fill("Threshold", "Text", It.IsAny<IReadOnlyList<IThresholdImplementationConfiguration>>(), It.IsAny<Func<IThresholdImplementationConfiguration, Guid>>(), It.IsAny<Action<IThresholdImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Record(First, "ScheduleFailureLimit")], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheReadPassesTheCallersTokenToTheFill()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill("Threshold", "Text", It.IsAny<IReadOnlyList<IThresholdImplementationConfiguration>>(), It.IsAny<Func<IThresholdImplementationConfiguration, Guid>>(), It.IsAny<Action<IThresholdImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Record(First, "ScheduleFailureLimit")], null, cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAThresholdRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Threshold", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
