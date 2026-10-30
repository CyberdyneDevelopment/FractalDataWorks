using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers <see cref="DefaultCalculationInputResolver"/>: a Scalar input resolves to its serialized value, a DataSet input to the rows
/// the row reader returns for the cap the host states (and is refused with CALCULATIONS-21027 when it states none, or 61021 when no reader
/// is registered), and a Container input is refused with CALCULATIONS-21022, because nothing reads one yet.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class DefaultCalculationInputResolverTests
{
    private static DefaultCalculationInputResolver Resolver()
        => new(NullLogger<DefaultCalculationInputResolver>.Instance);

    private static Task<IGenericResult<IReadOnlyList<ResolvedCalculationInput>>> Resolve(params CalculationInput[] inputs)
        => Resolver().Resolve(inputs, Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task ScalarInputResolvesToItsSerializedValue()
    {
        var scalar = new CalculationScalarValue { ValueType = ScalarValueTypes.ByName("Decimal"), SerializedValue = "4.5" };

        var result = await Resolve(CalculationInput.FromScalar(scalar, "rate"));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].InputAlias.ShouldBe("rate");
        result.Value[0].ResolvedValue.ShouldBe("4.5");
    }

    private static Mock<ICalculationContext> ContextWith(Mock<IDataGateway> gateway, int? rowCap)
    {
        var context = new Mock<ICalculationContext>();
        context.SetupGet(c => c.DataGateway).Returns(gateway.Object);
        context.SetupGet(c => c.Parameters).Returns(
            rowCap is { } cap
                ? new Dictionary<string, object?> { [CalculationDataSetRead.RowCapParameterName] = cap }
                : new Dictionary<string, object?>());
        return context;
    }

    [Fact]
    public async Task DataSetInputResolvesToTheRowsTheReaderReadsThroughTheContextsGateway()
    {
        var gateway = new Mock<IDataGateway>();
        IReadOnlyList<Dictionary<string, object?>> rows = [new() { ["Position"] = "QB", ["WeightLbs"] = 220 }];
        var reader = new Mock<IDataSetRowReader>();
        reader
            .Setup(r => r.Read(gateway.Object, "AfcPlayers", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Success(rows));
        var resolver = new DefaultCalculationInputResolver(NullLogger<DefaultCalculationInputResolver>.Instance, reader.Object);

        var result = await resolver.Resolve(
            [CalculationInput.FromDataSet("AfcPlayers", "players")], ContextWith(gateway, 100).Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].InputAlias.ShouldBe("players");
        result.Value[0].ResolvedValue.ShouldBeAssignableTo<IEnumerable<Dictionary<string, object>>>()!.Count().ShouldBe(1);
    }

    [Fact]
    public async Task DataSetInputWhoseHostStatesNoRowCapHasNoCallerAndIsRefused()
    {
        var gateway = new Mock<IDataGateway>();
        var reader = new Mock<IDataSetRowReader>();
        var resolver = new DefaultCalculationInputResolver(NullLogger<DefaultCalculationInputResolver>.Instance, reader.Object);

        var result = await resolver.Resolve(
            [CalculationInput.FromDataSet("AfcPlayers", "players")], ContextWith(gateway, null).Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21027");
        result.Messages[^1].Message.ShouldContain("players");
        reader.Verify(
            r => r.Read(It.IsAny<IDataGateway>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DataSetInputWithoutARegisteredReaderIsRefused()
    {
        var result = await Resolver().Resolve(
            [CalculationInput.FromDataSet("AfcPlayers", "players")], ContextWith(new Mock<IDataGateway>(), 100).Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-61021");
    }

    [Fact]
    public async Task ADataSetInputTheReaderRefusesPassesTheReadersFailureOn()
    {
        var gateway = new Mock<IDataGateway>();
        var reader = new Mock<IDataSetRowReader>();
        reader
            .Setup(r => r.Read(gateway.Object, "Big", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(new GenericMessage("over the cap")));
        var resolver = new DefaultCalculationInputResolver(NullLogger<DefaultCalculationInputResolver>.Instance, reader.Object);

        var result = await resolver.Resolve(
            [CalculationInput.FromDataSet("Big", "rows")], ContextWith(gateway, 100).Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("over the cap");
    }

    [Fact]
    public async Task ContainerInputIsRefusedAndNamesTheInputAndKind()
    {
        var result = await Resolve(CalculationInput.FromContainer("NflDb", "NflData.AfcPlayer", "players"));

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21022");
        result.Messages[^1].Message.ShouldContain("Container");
    }

    [Fact]
    public async Task AContainerInputBesideAScalarInputStillRefuses()
    {
        var scalar = new CalculationScalarValue { ValueType = ScalarValueTypes.ByName("Decimal"), SerializedValue = "1" };

        var result = await Resolve(CalculationInput.FromScalar(scalar, "rate"), CalculationInput.FromContainer("NflDb", "NflData.AfcPlayer", "players"));

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21022");
    }
}
