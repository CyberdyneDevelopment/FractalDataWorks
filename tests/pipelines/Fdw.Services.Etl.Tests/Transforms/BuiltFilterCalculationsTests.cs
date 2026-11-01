using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// FDW-2333: a Built transformation evaluated for a query filter, in process, with the values the filter supplies for its inputs.
/// The clock is fixed, so the date-time answers are exact.
/// </summary>
public sealed class BuiltFilterCalculationsTests
{
    private const string Chicago = "America/Chicago";
    private static readonly CalculationLimits Limits = new(200, 5000, 32);
    private static readonly DateTimeOffset ClockNow = new(2026, 10, 22, 14, 30, 5, TimeSpan.Zero);

    private static readonly IReadOnlyList<BuiltTransformationInputConfiguration> NoInputs = [];

    private static readonly IReadOnlyList<BuiltTransformationInputConfiguration> Seconds =
        [new BuiltTransformationInputConfiguration { Name = "seconds", ValueType = "Number" }];

    private static BuiltTransformationConfiguration Built(
        string name, IReadOnlyList<BuiltTransformationInputConfiguration> inputs, params (string Result, CalculationNode Node, bool Keep)[] results)
    {
        var array = results.Aggregate(
            CalculationArray.Named(name), (builder, result) => builder.Result(result.Result, result.Node, result.Keep));
        var built = BuiltTransformationConverter.ToConfiguration(name, array.Build(Limits, allowDateTimeResults: true).Value!, inputs);
        built.Id = Guid.NewGuid();
        return built;
    }

    private static BuiltTransformationConfiguration Yesterday() =>
        Built("Yesterday", NoInputs, ("when", AddTime(Now(Chicago, "StartOfDay"), -1, Chicago, "Days"), true));

    private static BuiltTransformationConfiguration SecondsAgo() =>
        Built("Seconds ago", Seconds, ("when", AddTime(Now("UTC"), Negate(Field("seconds")), "UTC", "Seconds"), true));

    private static BuiltFilterCalculations Calculations(params ITransformationImplementationConfiguration[] known)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) =>
                known.FirstOrDefault(item => item.Name == name) is { } found
                    ? GenericResult<ITransformationImplementationConfiguration>.Success(found)
                    : GenericResult<ITransformationImplementationConfiguration>.Failure(new GenericMessage("not found")));
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                known.FirstOrDefault(item => item.Id == id) is { } found
                    ? GenericResult<ITransformationImplementationConfiguration>.Success(found)
                    : GenericResult<ITransformationImplementationConfiguration>.Failure(new GenericMessage("not found")));
        return new BuiltFilterCalculations(provider.Object, Settings().Object, new FixedClock(ClockNow), NullLogger<BuiltFilterCalculations>.Instance);
    }

    private static Mock<IEffectiveSettingsProvider> Settings()
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(200);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(5000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(32);
        return settings;
    }

    private static Dictionary<string, object?> With(params (string Name, object? Value)[] inputs)
        => inputs.ToDictionary(input => input.Name, input => input.Value, StringComparer.Ordinal);

    [Fact]
    public async Task ACalculationWithNoInputsRunsAndYieldsADateTime()
    {
        var value = await Calculations(Yesterday()).Evaluate("Yesterday", With(), TestContext.Current.CancellationToken);

        value.IsSuccess.ShouldBeTrue();
        value.Value!.ValueType.ShouldBe(typeof(DateTimeOffset));
        value.Value.Value.ShouldBe(new DateTimeOffset(2026, 10, 21, 5, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task TheValueTheFilterSuppliesIsTheCalculationsInput()
    {
        var value = await Calculations(SecondsAgo()).Evaluate("Seconds ago", With(("seconds", 33m)), TestContext.Current.CancellationToken);

        value.IsSuccess.ShouldBeTrue();
        value.Value!.Value.ShouldBe(new DateTimeOffset(2026, 10, 22, 14, 29, 32, TimeSpan.Zero));
    }

    [Fact]
    public async Task ACalculationThatYieldsANumberSaysSo()
    {
        var plain = Built("Double", Seconds, ("twice", Multiply(Field("seconds"), 2), true));

        var value = await Calculations(plain).Evaluate("Double", With(("seconds", 4m)), TestContext.Current.CancellationToken);

        value.Value!.ValueType.ShouldBe(typeof(decimal));
        value.Value.Value.ShouldBe(8m);
    }

    [Fact]
    public async Task AnInputWithNoValueFailsNamingTheInputAndTheCalculation()
    {
        var value = await Calculations(SecondsAgo()).Evaluate("Seconds ago", With(), TestContext.Current.CancellationToken);

        value.IsFailure.ShouldBeTrue();
        value.CurrentMessage.ShouldContain("seconds");
        value.CurrentMessage.ShouldContain("Seconds ago");
    }

    [Fact]
    public async Task AValueForAnInputThatIsNotDeclaredFails()
    {
        var value = await Calculations(Yesterday()).Evaluate("Yesterday", With(("days", 3m)), TestContext.Current.CancellationToken);

        value.IsFailure.ShouldBeTrue();
        value.CurrentMessage.ShouldContain("days");
    }

    [Fact]
    public async Task AValueOfTheWrongTypeForAnInputFails()
    {
        var value = await Calculations(SecondsAgo()).Evaluate("Seconds ago", With(("seconds", true)), TestContext.Current.CancellationToken);

        value.IsFailure.ShouldBeTrue();
        value.CurrentMessage.ShouldContain("seconds");
    }

    [Fact]
    public async Task ACalculationThatIsNotThereFailsNamingIt()
    {
        var value = await Calculations(Yesterday()).Evaluate("Tomorrow", With(), TestContext.Current.CancellationToken);

        value.IsFailure.ShouldBeTrue();
        value.CurrentMessage.ShouldContain("Tomorrow");
    }

    [Fact]
    public async Task ATransformationThatIsNotBuiltFailsNamingIt()
    {
        var mapped = new Mock<ITransformationImplementationConfiguration>();
        mapped.SetupGet(m => m.Name).Returns("Remap");
        mapped.SetupGet(m => m.Id).Returns(Guid.NewGuid());
        mapped.SetupGet(m => m.Implementation).Returns("Map");

        var value = await Calculations(mapped.Object).Evaluate("Remap", With(), TestContext.Current.CancellationToken);

        value.IsFailure.ShouldBeTrue();
        value.CurrentMessage.ShouldContain("Remap");
    }

    [Fact]
    public async Task ACalculationThatKeepsMoreThanOneResultFails()
    {
        var two = Built("Two", NoInputs, ("a", Constant(1), true), ("b", Constant(2), true));

        var value = await Calculations(two).Evaluate("Two", With(), TestContext.Current.CancellationToken);

        value.IsFailure.ShouldBeTrue();
        value.CurrentMessage.ShouldContain("Two");
    }

    [Fact]
    public async Task ACalculationThatFailsWhileItRunsReturnsItsOwnCodedFailure()
    {
        var divide = Built("Share", Seconds, ("share", Divide(1, Field("seconds")), true));

        var value = await Calculations(divide).Evaluate("Share", With(("seconds", 0m)), TestContext.Current.CancellationToken);

        value.IsFailure.ShouldBeTrue();
        value.Code?.Name.ShouldBe("DivisionByZero");
    }

    [Fact]
    public async Task AComputeStageRefusesACalculationThatYieldsADateTime()
    {
        var yesterday = Yesterday();
        var stage = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Stamp",
            OperationType = "Compute",
            ComputeTransformationId = yesterday.Id,
        };
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(yesterday.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITransformationImplementationConfiguration>.Success(yesterday));

        var resolved = await BuiltCalculationResolver.Resolve(
            [stage], provider.Object, Settings().Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
        resolved.Code?.Name.ShouldBe("OperandTypeMismatch");
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedClock(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
