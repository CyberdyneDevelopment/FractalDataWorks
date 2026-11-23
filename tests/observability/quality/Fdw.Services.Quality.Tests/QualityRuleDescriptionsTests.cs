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
/// A quality rule's description, and that of each reference value, live in the descriptions table and in neither quality table (FDW-6208): the provider describes the rule in the unit
/// of its save with its values owned by it, fills both once for the set it reads, and removes them with the rule. The writer and reader are strict stand-ins.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class QualityRuleDescriptionsTests
{
    private static readonly Guid RuleId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid ValueA = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid ValueB = Guid.Parse("00000000-0000-0000-0000-0000000000b2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private QualityRuleConfigurationProvider Provider()
        => new(
            NullLogger<QualityRuleConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "quality", "QualityRule",
            _writer.Object, _reader.Object);

    private static QualityRuleImplementationConfiguration Rule(string? description, params (Guid Id, string Name, string? Description)[] values)
        => new()
        {
            Id = RuleId,
            Name = "no-null-ids",
            Description = description,
            ReferenceValues = values.Select(v => new QualityRuleReferenceValueConfiguration { Id = v.Id, Name = v.Name, Value = "x", Description = v.Description }).ToList(),
        };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IQualityRuleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IQualityRuleImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IQualityRuleImplementationConfiguration>>();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(typeof(QualityRuleImplementationConfiguration))]
    [InlineData(typeof(QualityRuleReferenceValueConfiguration))]
    public void NeitherRecordsDescriptionIsStored(Type record)
    {
        record.GetProperty("Description").ShouldNotBeNull().GetCustomAttribute<NotStoredAttribute>().ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedRuleIsDescribedAndItsValuesAreHandedOverWithTheRuleAsTheirOwner()
    {
        _writer
            .Setup(w => w.Describe("QualityRule", "Text", RuleId, "no-null-ids", "Ids are never null", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _writer
            .Setup(w => w.DescribeOwned(
                "QualityRuleReferenceValue", "Text", It.Is<DescriptionOwner>(o => o.Id == RuleId && o.Name == "no-null-ids"),
                It.Is<IReadOnlyList<OwnedDescription>>(things => things.SequenceEqual(new[] { new OwnedDescription(ValueA, "ok", "A good id"), new OwnedDescription(ValueB, "bad", null) })),
                It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Rule("Ids are never null", (ValueA, "ok", "A good id"), (ValueB, "bad", null)), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedRuleDescriptionFailsTheSaveAndTheValuesAreNotWritten()
    {
        _writer
            .Setup(w => w.Describe("QualityRule", "Text", RuleId, "no-null-ids", "Ids are never null", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Rule("Ids are never null", (ValueA, "ok", "A good id")), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadFillsTheRuleAndTheValuesOfTheSetFromTwoReads()
    {
        var owner = new DescriptionOwner(RuleId, "no-null-ids");
        IReadOnlyDictionary<Guid, DescriptionEntry> held = new Dictionary<Guid, DescriptionEntry>
        {
            [ValueA] = new(new DescriptionKey("QualityRuleReferenceValue", "Text", ValueA, "en-US"), "ok", owner, "A good id"),
        };
        _reader
            .Setup(r => r.Fill(
                "QualityRule", "Text", It.IsAny<IReadOnlyList<IQualityRuleImplementationConfiguration>>(), It.IsAny<Func<IQualityRuleImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IQualityRuleImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IQualityRuleImplementationConfiguration>, Func<IQualityRuleImplementationConfiguration, Guid>, Action<IQualityRuleImplementationConfiguration, string>, CancellationToken>(
                (_, _, rules, idOf, fill, _) => fill(rules.Single(), "Ids are never null"))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.ReadOwnedBy("QualityRuleReferenceValue", "Text", It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { RuleId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(held));
        var rule = Rule(null, (ValueA, "ok", null), (ValueB, "bad", null));

        var read = await Provider().Read([rule], null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        rule.Description.ShouldBe("Ids are never null");
        rule.ReferenceValues[0].Description.ShouldBe("A good id");
        rule.ReferenceValues[1].Description.ShouldBeNull();
        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARuleThatHoldsNoValueCostsNoReadOfValueTexts()
    {
        _reader
            .Setup(r => r.Fill(
                "QualityRule", "Text", It.IsAny<IReadOnlyList<IQualityRuleImplementationConfiguration>>(), It.IsAny<Func<IQualityRuleImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IQualityRuleImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read([Rule(null)], null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfTheValueTextsFailsTheRead()
    {
        _reader
            .Setup(r => r.Fill(
                "QualityRule", "Text", It.IsAny<IReadOnlyList<IQualityRuleImplementationConfiguration>>(), It.IsAny<Func<IQualityRuleImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IQualityRuleImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _reader
            .Setup(r => r.ReadOwnedBy("QualityRuleReferenceValue", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Rule(null, (ValueA, "ok", null))], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingARuleRemovesItsDescriptionAndThoseOfItsValuesInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("QualityRule", RuleId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteOwnedBy("QualityRuleReferenceValue", RuleId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(RuleId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        _writer.VerifyAll();
    }
}
