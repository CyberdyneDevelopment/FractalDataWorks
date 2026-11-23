using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Transformations;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A transformation's description lives in the descriptions table and in none of the seven kinds' tables (FDW-6208): the transformation provider describes the
/// transformation in the unit of its save, fills the text once for every set it reads, and removes it with the transformation. The writer and reader are strict
/// stand-ins, so what is asserted is what the provider asks of them and what it does with their answer; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class TransformationDescriptionsTests
{
    private static readonly Guid Trim = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Active = Guid.Parse("00000000-0000-0000-0000-0000000000a2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private TransformationConfigurationProvider Provider()
        => new(
            NullLogger<TransformationConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "transform", "Transformation",
            _writer.Object, _reader.Object);

    private static FilterTransformationConfiguration Filter(Guid id, string name, string? description = null)
        => new() { Id = id, Name = name, Description = description, Implementation = FilterTransformationConfiguration.ImplementationName };

    // The reader answers with the texts it holds, put on each transformation through the provider's own Id and fill delegates.
    private void TheReaderHolds(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill(
                "Transformation", "Text", It.IsAny<IReadOnlyList<ITransformationImplementationConfiguration>>(), It.IsAny<Func<ITransformationImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITransformationImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<ITransformationImplementationConfiguration>, Func<ITransformationImplementationConfiguration, Guid>, Action<ITransformationImplementationConfiguration, string>, CancellationToken>(
                (_, _, transformations, idOf, fill, _) =>
                {
                    foreach (var transformation in transformations)
                    {
                        if (texts.TryGetValue(idOf(transformation), out var text)) fill(transformation, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<ITransformationImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<ITransformationImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<ITransformationImplementationConfiguration>>();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(typeof(FieldTransformationConfiguration))]
    [InlineData(typeof(MapTransformationConfiguration))]
    [InlineData(typeof(FilterTransformationConfiguration))]
    [InlineData(typeof(CalculateTransformationConfiguration))]
    [InlineData(typeof(BuiltTransformationConfiguration))]
    [InlineData(typeof(AggregateTransformationConfiguration))]
    [InlineData(typeof(LookupTransformationConfiguration))]
    public void NoKindsDescriptionIsStoredInItsOwnTable(Type kind)
    {
        // The member stays, so every caller still gets the text; the column is gone, so no read selects it and no save writes it.
        var description = kind.GetProperty("Description").ShouldNotBeNull();

        description.GetCustomAttribute<NotStoredAttribute>().ShouldNotBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Keeps the rows a person trimmed")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedTransformationIsDescribedUnderTheTransformationTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Transformation", "Text", Trim, "trim", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Filter(Trim, "trim", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Transformation", "Text", Trim, "trim", "Keeps the rows a person trimmed", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Filter(Trim, "trim", "Keeps the rows a person trimmed"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryTransformationOfTheSetAndOneWithNoneStaysEmpty()
    {
        var transformations = new List<ITransformationImplementationConfiguration> { Filter(Trim, "trim"), Filter(Active, "active") };
        TheReaderHolds(new Dictionary<Guid, string> { [Trim] = "Keeps the rows a person trimmed" });

        var read = await Provider().Read(transformations, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        transformations[0].Description.ShouldBe("Keeps the rows a person trimmed");
        transformations[1].Description.ShouldBeNull();
        _reader.Verify(
            r => r.Fill(
                "Transformation", "Text", It.IsAny<IReadOnlyList<ITransformationImplementationConfiguration>>(), It.IsAny<Func<ITransformationImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITransformationImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheTransformationRead()
    {
        _reader
            .Setup(r => r.Fill(
                "Transformation", "Text", It.IsAny<IReadOnlyList<ITransformationImplementationConfiguration>>(), It.IsAny<Func<ITransformationImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITransformationImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Filter(Trim, "trim")], null, TestContext.Current.CancellationToken);

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
            .Setup(r => r.Fill(
                "Transformation", "Text", It.IsAny<IReadOnlyList<ITransformationImplementationConfiguration>>(), It.IsAny<Func<ITransformationImplementationConfiguration, Guid>>(),
                It.IsAny<Action<ITransformationImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Filter(Trim, "trim")], null, cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingATransformationRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Transformation", Trim, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(Trim, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }
}
