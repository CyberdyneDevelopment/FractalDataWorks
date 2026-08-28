using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Calculations.Tests.TestSupport;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers <see cref="CalculationEntityService"/> — the calculation domain's "chain executor": Get →
/// resolve inputs → dispatch to the registered <see cref="ICalculationEntityType"/> → return. Also
/// covers the CRUD surface (Create/Update/Delete/List/Validate) and the private BuildAggregate/
/// MapToEntity helpers exercised through them.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationEntityServiceTests
{
    private static Mock<CalculationConfigurationProvider> CreateProviderMock()
        => new(
            NullLogger<CalculationConfigurationProvider>.Instance,
            new ConfigurationGatewayProvider(),
            "ConfigurationDb",
            "calc");

    private static CalculationEntityConfiguration SampleConfig(Guid id, string name = "Calc1", string type = "Formula") => new()
    {
        Id = id,
        Name = name,
        Description = "desc",
        CalculationEntityType = type,
        OutputDataSetName = "OutDs",
        ResultFieldName = "Result",
        ResultDataTypeName = "Decimal",
        IsEnabled = true,
        Inputs =
        [
            new CalculationEntityInputRecord { InputAlias = "A", InputKind = "DataSet", DataSetName = "Ds1", Ordinal = 0 }
        ],
        Steps = []
    };

    // ---- GetCalculation(name) ----

    [Fact]
    public async Task GetCalculationSuccessReturnsMappedEntity()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(id)));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculation("Calc1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(id);
        result.Value.Name.ShouldBe("Calc1");
        result.Value.Description.ShouldBe("desc");
        result.Value.CalculationEntityType.ShouldBe("Formula");
        result.Value.Inputs.Count.ShouldBe(1);
        result.Value.Inputs[0].InputAlias.ShouldBe("A");
        result.Value.Inputs[0].Kind.ShouldBe(CalculationInputKinds.ByName("DataSet"));
        result.Value.Output.OutputDataSetName.ShouldBe("OutDs");
        result.Value.Output.ResultFieldName.ShouldBe("Result");
        result.Value.IsEnabled.ShouldBeTrue();
        result.Value.TypedConfiguration.ShouldBeNull();
    }

    [Fact]
    public async Task GetCalculationMapsScalarInputWithValueType()
    {
        var providerMock = CreateProviderMock();
        var config = SampleConfig(Guid.NewGuid());
        config.Inputs =
        [
            new CalculationEntityInputRecord
            {
                InputAlias = "Lit",
                InputKind = "Scalar",
                ScalarValueTypeName = "Decimal",
                ScalarValue = "3.14",
                Ordinal = 0
            }
        ];
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(config));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculation("Calc1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var input = result.Value!.Inputs[0];
        input.Kind.ShouldBe(CalculationInputKinds.ByName("Scalar"));
        input.ScalarValue.ShouldNotBeNull();
        input.ScalarValue!.ValueType.ShouldBe(ScalarValueTypes.ByName("Decimal"));
        input.ScalarValue.SerializedValue.ShouldBe("3.14");
    }

    [Fact]
    public async Task GetCalculationProviderFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Failure(new GenericMessage("boom")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculation("Missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    public async Task GetCalculationNullValueReturnsCalculationNotFound()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(null!));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculation("Missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-31000");
    }

    [Fact]
    public async Task GetCalculationThrowsReturnsCalculationLoadFailed()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculation("Calc1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71010");
    }

    // ---- GetCalculationById(id) ----

    [Fact]
    public async Task GetCalculationByIdSuccessReturnsMappedEntity()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(id)));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculationById(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetCalculationByIdProviderFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Failure(new GenericMessage("boom")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculationById(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetCalculationByIdNullValueReturnsCalculationNotFound()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(null!));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculationById(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-31000");
    }

    [Fact]
    public async Task GetCalculationByIdThrowsReturnsCalculationLoadFailed()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.GetCalculationById(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71010");
    }

    // ---- ListCalculations ----

    [Fact]
    public async Task ListCalculationsComposesFullAggregatePerHeader()
    {
        var providerMock = CreateProviderMock();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationEntityConfiguration>>.Success(
                [SampleConfig(id1, "Calc1"), SampleConfig(id2, "Calc2")]));
        providerMock.Setup(p => p.Get(id1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(id1, "Calc1")));
        providerMock.Setup(p => p.Get(id2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(id2, "Calc2")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        result.Value[0].Name.ShouldBe("Calc1");
        result.Value[1].Name.ShouldBe("Calc2");
    }

    [Fact]
    public async Task ListCalculationsHeadersFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationEntityConfiguration>>.Failure(new GenericMessage("boom")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        providerMock.Verify(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListCalculationsFullReadFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        var id1 = Guid.NewGuid();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationEntityConfiguration>>.Success([SampleConfig(id1)]));
        providerMock.Setup(p => p.Get(id1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Failure(new GenericMessage("boom")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ListCalculationsSkipsHeaderWhenFullReadReturnsNull()
    {
        var providerMock = CreateProviderMock();
        var id1 = Guid.NewGuid();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationEntityConfiguration>>.Success([SampleConfig(id1)]));
        providerMock.Setup(p => p.Get(id1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(null!));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ListCalculationsThrowsReturnsListCalculationsFailed()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71009");
    }

    // ---- ValidateCalculation ----

    [Fact]
    public async Task ValidateCalculationUnknownTypeReturnsFailure()
    {
        var providerMock = CreateProviderMock();
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);
        var entity = new TestCalculationEntity { CalculationEntityType = "Bogus" };

        var result = await service.ValidateCalculation(entity, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Unknown calculation entity type");
    }

    [Theory]
    [InlineData("Formula")]
    [InlineData("Windowed")]
    public async Task ValidateCalculationKnownTypeReturnsSuccess(string entityType)
    {
        var providerMock = CreateProviderMock();
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);
        var entity = new TestCalculationEntity { CalculationEntityType = entityType };

        var result = await service.ValidateCalculation(entity, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateCalculationThrowsReturnsValidateCalculationFailed()
    {
        var providerMock = CreateProviderMock();
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);
        var entity = new ThrowsOnceCalculationEntity();

        var result = await service.ValidateCalculation(entity, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21001");
    }

    // ---- CreateCalculation ----

    [Fact]
    public async Task CreateCalculationUnknownEntityTypeReturnsFailureWithoutSaving()
    {
        var providerMock = CreateProviderMock();
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.CreateCalculation(
            "Name", null, "Bogus", [], new CalculationOutputSpec(), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-41000");
        providerMock.Verify(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCalculationInputMissingKindReturnsFailureWithoutSaving()
    {
        var providerMock = CreateProviderMock();
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);
        var inputs = new List<CalculationInput> { new() { Kind = null!, InputAlias = "A" } };

        var result = await service.CreateCalculation(
            "Name", null, "Formula", inputs, new CalculationOutputSpec(), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Kind is required");
        providerMock.Verify(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCalculationTypedConfigurationNotCalculationTypedReturnsFailure()
    {
        var providerMock = CreateProviderMock();
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.CreateCalculation(
            "Name", null, "Formula", [], new CalculationOutputSpec(),
            Mock.Of<IGenericConfiguration>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("does not implement ICalculationTypedConfiguration");
        providerMock.Verify(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCalculationSaveFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Failure(new GenericMessage("save failed")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.CreateCalculation(
            "Name", null, "Formula", [], new CalculationOutputSpec(), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("save failed");
    }

    [Fact]
    public async Task CreateCalculationSuccessStampsTypedBodyIdEmptyAndReturnsMappedEntity()
    {
        var providerMock = CreateProviderMock();
        var savedId = Guid.NewGuid();
        CalculationEntityConfiguration? captured = null;
        providerMock.Setup(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback<CalculationEntityConfiguration, CancellationToken>((record, _) => captured = record)
            .ReturnsAsync((CalculationEntityConfiguration record, CancellationToken _) =>
                GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(savedId)));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);
        var typedConfig = new FormulaCalculationConfiguration { Id = Guid.NewGuid(), FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await service.CreateCalculation(
            "Name", "desc", "Formula", [], new CalculationOutputSpec(), typedConfig, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(savedId);
        captured.ShouldNotBeNull();
        captured!.Configuration.ShouldBeSameAs(typedConfig);
        typedConfig.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateCalculationThrowsReturnsCreateCalculationFailed()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.CreateCalculation(
            "Name", null, "Formula", [], new CalculationOutputSpec(), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71011");
    }

    // ---- UpdateCalculation ----

    [Fact]
    public async Task UpdateCalculationBuildFailureReturnsFailureWithoutDeleteOrSave()
    {
        var providerMock = CreateProviderMock();
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.UpdateCalculation(
            Guid.NewGuid(), "Name", null, "Bogus", [], new CalculationOutputSpec(), true, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        providerMock.Verify(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        providerMock.Verify(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCalculationNeverDeletesFirst()
    {
        // This replaces UpdateCalculationDeleteFailurePropagatesWithoutSave, which asserted the OLD
        // Delete-then-Save sequence: it stubbed Delete to fail and expected that failure to surface.
        // Save now version-on-writes and cascades the whole aggregate, so the delete step was removed
        // deliberately — and against the now fail-loud Delete (which errors when the record does not
        // exist rather than silently succeeding) keeping it would abort EVERY update. Inverted to guard
        // the invariant that replaced it: a failing Delete must be irrelevant, because Update must not
        // call it at all.
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Delete(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("delete failed")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.UpdateCalculation(
            id, "Name", null, "Formula", [], new CalculationOutputSpec(), true, null, TestContext.Current.CancellationToken);

        providerMock.Verify(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        providerMock.Verify(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        result.CurrentMessage.ShouldNotBe("delete failed", "a Delete that is never called cannot influence the outcome");
    }

    [Fact]
    public async Task UpdateCalculationSaveFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Delete(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        providerMock.Setup(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Failure(new GenericMessage("save failed")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.UpdateCalculation(
            id, "Name", null, "Formula", [], new CalculationOutputSpec(), true, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("save failed");
    }

    [Fact]
    public async Task UpdateCalculationSuccessReturnsMappedEntity()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Delete(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        providerMock.Setup(p => p.Save(It.IsAny<CalculationEntityConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(id, "Renamed")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.UpdateCalculation(
            id, "Renamed", null, "Formula", [], new CalculationOutputSpec(), true, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Renamed");
    }

    [Fact]
    public async Task UpdateCalculationThrowsReturnsUpdateCalculationFailed()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Delete(id, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.UpdateCalculation(
            id, "Name", null, "Formula", [], new CalculationOutputSpec(), true, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71012");
    }

    // ---- DeleteCalculation ----

    [Fact]
    public async Task DeleteCalculationSuccessReturnsSuccess()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Delete(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.DeleteCalculation(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteCalculationFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Delete(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("boom")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.DeleteCalculation(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteCalculationThrowsReturnsDeleteCalculationFailed()
    {
        var providerMock = CreateProviderMock();
        var id = Guid.NewGuid();
        providerMock.Setup(p => p.Delete(id, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.DeleteCalculation(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71013");
    }

    // ---- ExecuteCalculation (the calculation "chain": Get -> validate type -> resolve inputs -> dispatch Execute) ----

    [Fact]
    public async Task ExecuteCalculationGetCalculationFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Failure(new GenericMessage("not found")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("not found");
    }

    [Fact]
    public async Task ExecuteCalculationUnknownEntityTypeReturnsFailure()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(Guid.NewGuid(), type: "Bogus")));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Unknown calculation entity type");
    }

    [Fact]
    public async Task ExecuteCalculationInputResolutionFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(SampleConfig(Guid.NewGuid())));
        var resolverMock = new Mock<ICalculationInputResolver>();
        resolverMock.Setup(r => r.Resolve(It.IsAny<IReadOnlyList<CalculationInput>>(), It.IsAny<ICalculationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Failure(new GenericMessage("resolve failed")));
        var service = new CalculationEntityService(providerMock.Object, resolverMock.Object, null);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("resolve failed");
    }

    [Fact]
    public async Task ExecuteCalculationEntityExecuteFailurePropagates()
    {
        var providerMock = CreateProviderMock();
        var config = SampleConfig(Guid.NewGuid());
        config.Configuration = null;
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(config));
        var resolverMock = new Mock<ICalculationInputResolver>();
        resolverMock.Setup(r => r.Resolve(It.IsAny<IReadOnlyList<CalculationInput>>(), It.IsAny<ICalculationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Success([]));
        var service = new CalculationEntityService(providerMock.Object, resolverMock.Object, null);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-61000");
    }

    [Fact]
    public async Task ExecuteCalculationSuccessReturnsSerializedResult()
    {
        var providerMock = CreateProviderMock();
        var config = SampleConfig(Guid.NewGuid());
        config.Configuration = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityConfiguration>.Success(config));
        var resolverMock = new Mock<ICalculationInputResolver>();
        resolverMock.Setup(r => r.Resolve(It.IsAny<IReadOnlyList<CalculationInput>>(), It.IsAny<ICalculationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Success([]));
        var service = new CalculationEntityService(providerMock.Object, resolverMock.Object, null);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("\"RowCount\":0");
    }

    [Fact]
    public async Task ExecuteCalculationThrowsReturnsExecuteCalculationFailed()
    {
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.Get("Calc1", It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var service = new CalculationEntityService(providerMock.Object, Mock.Of<ICalculationInputResolver>(), null);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71010");
    }
}
