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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers <see cref="CalculationEntityService"/> — the calculation domain's "chain executor": Get →
/// resolve inputs → dispatch to the registered <see cref="ICalculationEntityType"/> → return. Also
/// covers the CRUD surface (Create/Update/Delete/List/Validate) and the private BuildAggregate/
/// MapToEntity helpers exercised through them.
/// </summary>
/// <remarks>
/// The implementation record IS the calculation: the kind a calculation is comes from that record's
/// type, and a write lands on the implementation provider the domain dispatches to. Each test states
/// what the store holds through <see cref="CalculationStore"/> and asserts against that provider.
/// </remarks>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationEntityServiceTests
{
    private static FormulaCalculationConfiguration SampleConfig(Guid id, string name = "Calc1") => new()
    {
        Id = id,
        Name = name,
        Description = "desc",
        Implementation = "Formula",
        FormulaBody = "1+1",
        FormulaLanguage = "CSharp",
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

    private static CalculationEntityService ServiceOver(
        CalculationStore store, ICalculationInputResolver? resolver = null)
        => new(store.Provider, resolver ?? Mock.Of<ICalculationInputResolver>(), null);

    // ---- GetCalculation(name) ----

    [Fact]
    public async Task GetCalculationSuccessReturnsMappedEntity()
    {
        var id = Guid.NewGuid();
        var config = SampleConfig(id);
        var service = ServiceOver(new CalculationStore().Holds(config));

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

        // The record read back IS the typed configuration; there is no separate body to carry.
        result.Value.TypedConfiguration.ShouldBeSameAs(config);
    }

    [Fact]
    public async Task GetCalculationMapsScalarInputWithValueType()
    {
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
        var service = ServiceOver(new CalculationStore().Holds(config));

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
        var service = ServiceOver(new CalculationStore().Unreadable("boom"));

        var result = await service.GetCalculation("Missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    public async Task GetCalculationNullValueReturnsCalculationNotFound()
    {
        var service = ServiceOver(new CalculationStore());

        var result = await service.GetCalculation("Missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-31000");
    }

    [Fact]
    public async Task GetCalculationThrowsReturnsCalculationLoadFailed()
    {
        var service = ServiceOver(new CalculationStore().Throws(new InvalidOperationException("db down")));

        var result = await service.GetCalculation("Calc1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71010");
    }

    // ---- GetCalculationById(id) ----

    [Fact]
    public async Task GetCalculationByIdSuccessReturnsMappedEntity()
    {
        var id = Guid.NewGuid();
        var service = ServiceOver(new CalculationStore().Holds(SampleConfig(id)));

        var result = await service.GetCalculationById(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetCalculationByIdProviderFailurePropagates()
    {
        var service = ServiceOver(new CalculationStore().Unreadable("boom"));

        var result = await service.GetCalculationById(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetCalculationByIdNullValueReturnsCalculationNotFound()
    {
        var service = ServiceOver(new CalculationStore());

        var result = await service.GetCalculationById(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-31000");
    }

    [Fact]
    public async Task GetCalculationByIdThrowsReturnsCalculationLoadFailed()
    {
        var service = ServiceOver(new CalculationStore().Throws(new InvalidOperationException("db down")));

        var result = await service.GetCalculationById(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71010");
    }

    // ---- ListCalculations ----

    [Fact]
    public async Task ListCalculationsComposesFullAggregatePerHeader()
    {
        var first = SampleConfig(Guid.NewGuid(), "Calc1");
        var second = SampleConfig(Guid.NewGuid(), "Calc2");

        // The list read, then the full read of each member it names.
        var service = ServiceOver(new CalculationStore().HoldsInTurn([first, second], [first], [second]));

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        result.Value[0].Name.ShouldBe("Calc1");
        result.Value[1].Name.ShouldBe("Calc2");
    }

    [Fact]
    public async Task ListCalculationsHeadersFailurePropagates()
    {
        var store = new CalculationStore().Unreadable("boom");
        var service = ServiceOver(store);

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        store.ReadRecordsTimes(Times.Never());
    }

    [Fact]
    public async Task ListCalculationsFullReadFailurePropagates()
    {
        var config = SampleConfig(Guid.NewGuid());
        var store = new CalculationStore().Holds(config);

        // The list read composes the member; the full read of that member then fails.
        store.RecordInTurn(
            config.Id,
            GenericResult<ICalculationEntityImplementationConfiguration>.Success(config),
            GenericResult<ICalculationEntityImplementationConfiguration>.Failure(new GenericMessage("boom")));

        var result = await ServiceOver(store).ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ListCalculationsSkipsHeaderWhenFullReadReturnsNull()
    {
        var config = SampleConfig(Guid.NewGuid());
        var store = new CalculationStore().Holds(config);
        store.RecordInTurn(
            config.Id,
            GenericResult<ICalculationEntityImplementationConfiguration>.Success(config),
            GenericResult<ICalculationEntityImplementationConfiguration>.Success(null!));

        var result = await ServiceOver(store).ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ListCalculationsThrowsReturnsListCalculationsFailed()
    {
        var service = ServiceOver(new CalculationStore().Throws(new InvalidOperationException("boom")));

        var result = await service.ListCalculations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71009");
    }

    // ---- ValidateCalculation ----

    [Fact]
    public async Task ValidateCalculationUnknownTypeReturnsFailure()
    {
        var service = ServiceOver(new CalculationStore());
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
        var service = ServiceOver(new CalculationStore());
        var entity = new TestCalculationEntity { CalculationEntityType = entityType };

        var result = await service.ValidateCalculation(entity, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateCalculationThrowsReturnsValidateCalculationFailed()
    {
        var service = ServiceOver(new CalculationStore());

        var result = await service.ValidateCalculation(new ThrowsOnceCalculationEntity(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21001");
    }

    // ---- CreateCalculation ----

    [Fact]
    public async Task CreateCalculationUnknownEntityTypeReturnsFailureWithoutSaving()
    {
        var store = new CalculationStore();

        var result = await ServiceOver(store).CreateCalculation(
            "Name", null, "Bogus", [], new CalculationOutputSpec(), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-41000");
        store.SavedTimes(Times.Never());
    }

    [Fact]
    public async Task CreateCalculationInputMissingKindReturnsFailureWithoutSaving()
    {
        var store = new CalculationStore();
        var inputs = new List<CalculationInput> { new() { Kind = null!, InputAlias = "A" } };

        var result = await ServiceOver(store).CreateCalculation(
            "Name", null, "Formula", inputs, new CalculationOutputSpec(), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Kind is required");
        store.SavedTimes(Times.Never());
    }

    [Fact]
    public async Task CreateCalculationWithoutATypedConfigurationReturnsFailureWithoutSaving()
    {
        // The implementation is the record that gets written, so there is nothing to write without it.
        var store = new CalculationStore();

        var result = await ServiceOver(store).CreateCalculation(
            "Name", null, "Formula", [], new CalculationOutputSpec(), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("requires its typed configuration");
        store.SavedTimes(Times.Never());
    }

    [Fact]
    public async Task CreateCalculationTypedConfigurationNotCalculationTypedReturnsFailure()
    {
        var store = new CalculationStore();

        var result = await ServiceOver(store).CreateCalculation(
            "Name", null, "Formula", [], new CalculationOutputSpec(),
            Mock.Of<IGenericConfiguration>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("does not implement ICalculationTypedConfiguration");
        store.SavedTimes(Times.Never());
    }

    [Fact]
    public async Task CreateCalculationSaveFailurePropagates()
    {
        var store = new CalculationStore().SaveFails("save failed");
        var typedConfig = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await ServiceOver(store).CreateCalculation(
            "Name", null, "Formula", [], new CalculationOutputSpec(), typedConfig, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("save failed");
    }

    [Fact]
    public async Task CreateCalculationWritesTheTypedConfigurationAndReturnsWhatWasSaved()
    {
        var saved = SampleConfig(Guid.NewGuid(), "Name");
        ICalculationEntityImplementationConfiguration? written = null;
        var store = new CalculationStore().Holds(saved).Saves(record => written = record);
        var typedConfig = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await ServiceOver(store).CreateCalculation(
            "Name", "desc", "Formula", [], new CalculationOutputSpec(), typedConfig, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(saved.Id);

        // The record handed in is the record written — not a copy, and not a body hung off one.
        written.ShouldBeSameAs(typedConfig);
        written!.Name.ShouldBe("Name");
        written.Description.ShouldBe("desc");
    }

    [Fact]
    public async Task CreateCalculationThrowsReturnsCreateCalculationFailed()
    {
        var store = new CalculationStore().SaveThrows(new InvalidOperationException("boom"));
        var typedConfig = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await ServiceOver(store).CreateCalculation(
            "Name", null, "Formula", [], new CalculationOutputSpec(), typedConfig, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71011");
    }

    // ---- UpdateCalculation ----

    [Fact]
    public async Task UpdateCalculationBuildFailureReturnsFailureWithoutDeleteOrSave()
    {
        var store = new CalculationStore();

        var result = await ServiceOver(store).UpdateCalculation(
            Guid.NewGuid(), "Name", null, "Bogus", [], new CalculationOutputSpec(), true, null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        store.DeletedTimes(Times.Never());
        store.SavedTimes(Times.Never());
    }

    [Fact]
    public async Task UpdateCalculationNeverDeletesFirst()
    {
        // This replaces UpdateCalculationDeleteFailurePropagatesWithoutSave, which asserted the OLD
        // Delete-then-Save sequence. Save now version-on-writes and cascades the whole aggregate, so
        // the delete step was removed deliberately — and against the now fail-loud Delete keeping it
        // would abort EVERY update. Inverted to guard the invariant that replaced it: a failing
        // Delete must be irrelevant, because Update must not call it at all.
        var saved = SampleConfig(Guid.NewGuid(), "Name");
        var store = new CalculationStore().Holds(saved).Saves().DeleteFails("delete failed");
        var typedConfig = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await ServiceOver(store).UpdateCalculation(
            saved.Id, "Name", null, "Formula", [], new CalculationOutputSpec(), true, typedConfig,
            TestContext.Current.CancellationToken);

        store.DeletedTimes(Times.Never());
        store.SavedTimes(Times.Once());
        result.CurrentMessage.ShouldNotBe("delete failed", "a Delete that is never called cannot influence the outcome");
    }

    [Fact]
    public async Task UpdateCalculationSaveFailurePropagates()
    {
        var store = new CalculationStore().SaveFails("save failed");
        var typedConfig = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await ServiceOver(store).UpdateCalculation(
            Guid.NewGuid(), "Name", null, "Formula", [], new CalculationOutputSpec(), true, typedConfig,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("save failed");
    }

    [Fact]
    public async Task UpdateCalculationSuccessReturnsMappedEntity()
    {
        var saved = SampleConfig(Guid.NewGuid(), "Renamed");
        var store = new CalculationStore().Holds(saved).Saves();
        var typedConfig = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await ServiceOver(store).UpdateCalculation(
            saved.Id, "Renamed", null, "Formula", [], new CalculationOutputSpec(), true, typedConfig,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Renamed");
    }

    [Fact]
    public async Task UpdateCalculationThrowsReturnsUpdateCalculationFailed()
    {
        var store = new CalculationStore().SaveThrows(new InvalidOperationException("boom"));
        var typedConfig = new FormulaCalculationConfiguration { FormulaBody = "1+1", FormulaLanguage = "CSharp" };

        var result = await ServiceOver(store).UpdateCalculation(
            Guid.NewGuid(), "Name", null, "Formula", [], new CalculationOutputSpec(), true, typedConfig,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71012");
    }

    // ---- DeleteCalculation ----

    [Fact]
    public async Task DeleteCalculationSuccessReturnsSuccess()
    {
        var config = SampleConfig(Guid.NewGuid());
        var store = new CalculationStore().Holds(config).Deletes();

        var result = await ServiceOver(store).DeleteCalculation(config.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteCalculationFailurePropagates()
    {
        var config = SampleConfig(Guid.NewGuid());
        var store = new CalculationStore().Holds(config).DeleteFails("boom");

        var result = await ServiceOver(store).DeleteCalculation(config.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteCalculationThrowsReturnsDeleteCalculationFailed()
    {
        var config = SampleConfig(Guid.NewGuid());
        var store = new CalculationStore().Holds(config).DeleteThrows(new InvalidOperationException("boom"));

        var result = await ServiceOver(store).DeleteCalculation(config.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71013");
    }

    // ---- ExecuteCalculation (the calculation "chain": Get -> validate type -> resolve inputs -> dispatch Execute) ----

    [Fact]
    public async Task ExecuteCalculationGetCalculationFailurePropagates()
    {
        var service = ServiceOver(new CalculationStore().Unreadable("not found"));

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("not found");
    }

    [Fact]
    public async Task ExecuteCalculationUnknownEntityTypeReturnsFailure()
    {
        // A record whose type no option claims: the kind is unknown because the TYPE is the kind.
        var service = ServiceOver(new CalculationStore().Holds(
            new UnregisteredCalculationConfiguration { Id = Guid.NewGuid(), Name = "Calc1" }));

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Unknown calculation entity type");
    }

    [Fact]
    public async Task ExecuteCalculationInputResolutionFailurePropagates()
    {
        var resolver = new Mock<ICalculationInputResolver>();
        resolver
            .Setup(r => r.Resolve(It.IsAny<IReadOnlyList<CalculationInput>>(), It.IsAny<ICalculationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Failure(new GenericMessage("resolve failed")));
        var service = ServiceOver(new CalculationStore().Holds(SampleConfig(Guid.NewGuid())), resolver.Object);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("resolve failed");
    }

    [Fact]
    public async Task ExecuteCalculationSuccessReturnsSerializedResult()
    {
        var resolver = new Mock<ICalculationInputResolver>();
        resolver
            .Setup(r => r.Resolve(It.IsAny<IReadOnlyList<CalculationInput>>(), It.IsAny<ICalculationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ResolvedCalculationInput>>.Success([]));
        var service = ServiceOver(new CalculationStore().Holds(SampleConfig(Guid.NewGuid())), resolver.Object);

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain("\"RowCount\":0");
    }

    [Fact]
    public async Task ExecuteCalculationThrowsReturnsExecuteCalculationFailed()
    {
        var service = ServiceOver(new CalculationStore().Throws(new InvalidOperationException("boom")));

        var result = await service.ExecuteCalculation("Calc1", Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-71010");
    }
}
