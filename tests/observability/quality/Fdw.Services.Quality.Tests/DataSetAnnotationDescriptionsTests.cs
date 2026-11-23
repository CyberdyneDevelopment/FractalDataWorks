using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Quality.Tests;

/// <summary>
/// A data set annotation's description, and the description it gives each field, live in the descriptions table and in neither catalog table (FDW-6208): the provider describes the
/// annotation in the unit of its save with its field descriptions owned by it, fills both once for the set it reads, and removes them with the annotation. The writer and reader are
/// strict stand-ins.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DataSetAnnotationDescriptionsTests
{
    private static readonly Guid AnnotationId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid FieldA = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid FieldB = Guid.Parse("00000000-0000-0000-0000-0000000000b2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private DataSetAnnotationConfigurationProvider Provider()
        => new(
            NullLogger<DataSetAnnotationConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "catalog", "DataSetAnnotation",
            _writer.Object, _reader.Object);

    private static DataSetAnnotationImplementationConfiguration Annotation(string? description, params (Guid Id, string Field, string Text)[] fields)
        => new()
        {
            Id = AnnotationId,
            Name = "Orders",
            DataSetName = "Orders",
            Description = description,
            FieldDescriptions = fields.Select(f => new DataSetAnnotationFieldDescriptionConfiguration { Id = f.Id, Name = f.Field, FieldName = f.Field, Description = f.Text }).ToList(),
        };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IDataSetAnnotationImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IDataSetAnnotationImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IDataSetAnnotationImplementationConfiguration>>();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(typeof(DataSetAnnotationImplementationConfiguration))]
    [InlineData(typeof(DataSetAnnotationFieldDescriptionConfiguration))]
    public void NeitherRecordsDescriptionIsStored(Type record)
    {
        record.GetProperty("Description").ShouldNotBeNull().GetCustomAttribute<NotStoredAttribute>().ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedAnnotationIsDescribedAndItsFieldDescriptionsAreHandedOverNamedByTheirField()
    {
        _writer
            .Setup(w => w.Describe("DataSetAnnotation", "Text", AnnotationId, "Orders", "Orders placed", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _writer
            .Setup(w => w.DescribeOwned(
                "DataSetAnnotationField", "Text", It.Is<DescriptionOwner>(o => o.Id == AnnotationId && o.Name == "Orders"),
                It.Is<IReadOnlyList<OwnedDescription>>(things => things.SequenceEqual(new[] { new OwnedDescription(FieldA, "Total", "The order total"), new OwnedDescription(FieldB, "Placed", string.Empty) })),
                It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Annotation("Orders placed", (FieldA, "Total", "The order total"), (FieldB, "Placed", string.Empty)), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedAnnotationDescriptionFailsTheSaveAndTheFieldsAreNotWritten()
    {
        _writer
            .Setup(w => w.Describe("DataSetAnnotation", "Text", AnnotationId, "Orders", "Orders placed", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Annotation("Orders placed", (FieldA, "Total", "The order total")), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadFillsTheAnnotationAndItsFieldsFromTwoReadsAndAFieldWithNoTextKeepsNone()
    {
        IReadOnlyDictionary<Guid, DescriptionEntry> held = new Dictionary<Guid, DescriptionEntry>
        {
            [FieldA] = new(new DescriptionKey("DataSetAnnotationField", "Text", FieldA, "en-US"), "Total", new DescriptionOwner(AnnotationId, "Orders"), "The order total"),
        };
        _reader
            .Setup(r => r.Fill(
                "DataSetAnnotation", "Text", It.IsAny<IReadOnlyList<IDataSetAnnotationImplementationConfiguration>>(), It.IsAny<Func<IDataSetAnnotationImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IDataSetAnnotationImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IDataSetAnnotationImplementationConfiguration>, Func<IDataSetAnnotationImplementationConfiguration, Guid>, Action<IDataSetAnnotationImplementationConfiguration, string>, CancellationToken>(
                (_, _, annotations, _, fill, _) => fill(annotations.Single(), "Orders placed"))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.ReadOwnedBy("DataSetAnnotationField", "Text", It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { AnnotationId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(held));
        var annotation = Annotation(null, (FieldA, "Total", string.Empty), (FieldB, "Placed", string.Empty));

        var read = await Provider().Read([annotation], null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        annotation.Description.ShouldBe("Orders placed");
        annotation.FieldDescriptions[0].Description.ShouldBe("The order total");
        annotation.FieldDescriptions[1].Description.ShouldBe(string.Empty);
        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnAnnotationThatHoldsNoFieldDescriptionCostsNoReadOfFieldTexts()
    {
        _reader
            .Setup(r => r.Fill(
                "DataSetAnnotation", "Text", It.IsAny<IReadOnlyList<IDataSetAnnotationImplementationConfiguration>>(), It.IsAny<Func<IDataSetAnnotationImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IDataSetAnnotationImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read([Annotation(null)], null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfTheFieldTextsFailsTheRead()
    {
        _reader
            .Setup(r => r.Fill(
                "DataSetAnnotation", "Text", It.IsAny<IReadOnlyList<IDataSetAnnotationImplementationConfiguration>>(), It.IsAny<Func<IDataSetAnnotationImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IDataSetAnnotationImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.ReadOwnedBy("DataSetAnnotationField", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Annotation(null, (FieldA, "Total", string.Empty))], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAnAnnotationRemovesItsDescriptionAndThoseOfItsFieldsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("DataSetAnnotation", AnnotationId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteOwnedBy("DataSetAnnotationField", AnnotationId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(AnnotationId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        _writer.VerifyAll();
    }
}
