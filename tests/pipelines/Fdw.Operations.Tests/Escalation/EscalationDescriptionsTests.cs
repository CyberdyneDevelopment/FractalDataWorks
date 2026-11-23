using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// An escalation policy's description lives in the descriptions table and not on <c>workflow.EscalationPolicyConfiguration</c> (FDW-7205, a group of FDW-6208): the provider describes the escalation policy in the unit of its save,
/// fills the text once for every set of composed escalation policies it reads, and removes it with the escalation policy. The text is kept under the Id of the domain row. The writer and reader
/// are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class EscalationDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000007d1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000007d2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private EscalationConfigurationProvider Provider()
        => new(
            NullLogger<EscalationConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "workflow", "EscalationPolicy",
            _writer.Object, _reader.Object);

    private static EscalationPolicyImplementationConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IEscalationPolicyImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IEscalationPolicyImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IEscalationPolicyImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheImplementationsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(EscalationPolicyImplementationConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Dev: tells the on-call channel that a pipeline was disabled")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedPolicyIsDescribedUnderTheTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("EscalationPolicy", "Text", First, "DevFailureAlerts", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "DevFailureAlerts", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("EscalationPolicy", "Text", First, "DevFailureAlerts", "Dev: tells the on-call channel that a pipeline was disabled", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "DevFailureAlerts", "Dev: tells the on-call channel that a pipeline was disabled"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedPolicyAndOneWithNoneStaysEmpty()
    {
        var things = new List<IEscalationPolicyImplementationConfiguration> { Record(First, "DevFailureAlerts"), Record(Second, "other") };
        _reader
            .Setup(r => r.Fill("EscalationPolicy", "Text", It.IsAny<IReadOnlyList<IEscalationPolicyImplementationConfiguration>>(), It.IsAny<Func<IEscalationPolicyImplementationConfiguration, Guid>>(), It.IsAny<Action<IEscalationPolicyImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IEscalationPolicyImplementationConfiguration>, Func<IEscalationPolicyImplementationConfiguration, Guid>, Action<IEscalationPolicyImplementationConfiguration, string>, CancellationToken>(
                (_, _, found, idOf, fill, _) =>
                {
                    foreach (var thing in found)
                    {
                        if (idOf(thing) == First) fill(thing, "Dev: tells the on-call channel that a pipeline was disabled");
                    }
                })
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Dev: tells the on-call channel that a pipeline was disabled");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfComposedPolicys()
    {
        _reader
            .Setup(r => r.Fill("EscalationPolicy", "Text", It.IsAny<IReadOnlyList<IEscalationPolicyImplementationConfiguration>>(), It.IsAny<Func<IEscalationPolicyImplementationConfiguration, Guid>>(), It.IsAny<Action<IEscalationPolicyImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Record(First, "DevFailureAlerts")], null, TestContext.Current.CancellationToken);

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
            .Setup(r => r.Fill("EscalationPolicy", "Text", It.IsAny<IReadOnlyList<IEscalationPolicyImplementationConfiguration>>(), It.IsAny<Func<IEscalationPolicyImplementationConfiguration, Guid>>(), It.IsAny<Action<IEscalationPolicyImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Record(First, "DevFailureAlerts")], null, cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAPolicyRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("EscalationPolicy", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
