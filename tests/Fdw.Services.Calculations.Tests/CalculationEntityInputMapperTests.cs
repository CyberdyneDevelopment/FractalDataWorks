using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// FDW-3228: create and update refuse an input that sends a member its kind does not declare, naming the members the kind takes; a member
/// that is null or empty is not sent, so a client that sends a read back is not refused. FDW-3246: they also refuse an input that leaves out
/// (null or empty) a member its kind requires, naming that member. FDW-3258: a scalar's value must be a value of its type.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationEntityInputMapperTests
{
    [Fact]
    public void ADataSetInputThatSendsOnlyItsDataSetIsMapped()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "players", InputKind = "DataSet", DataSetName = "AfcPlayers" }], NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldHaveSingleItem().DataSetName.ShouldBe("AfcPlayers");
    }

    [Fact]
    public void ADataSetInputThatSendsAScalarMemberIsRefusedNamingWhatItTakes()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "players", InputKind = "DataSet", DataSetName = "AfcPlayers", ScalarValueType = "Int32" }],
            NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        var refusal = result.Messages[^1];
        refusal.Code.ShouldBe("ENDPOINTS-21547");
        var text = refusal.ShouldBeAssignableTo<IClientMessage>().ClientText;
        text.ShouldContain("players");
        text.ShouldContain("scalarValueType");
        text.ShouldContain("dataSetName");
    }

    [Fact]
    public void AScalarInputThatSendsADataSetIsRefused()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto
            {
                InputAlias = "n", InputKind = "Scalar", DataSetName = "AfcPlayers", ScalarValueType = "Int32", ScalarSerializedValue = "5"
            }],
            NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21547");
    }

    [Fact]
    public void AContainerInputThatSendsItsConnectionAndPathIsMapped()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "c", InputKind = "Container", ConnectionName = "Main", ContainerPath = "dbo.Players" }],
            NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var input = result.Value.ShouldHaveSingleItem();
        input.ConnectionName.ShouldBe("Main");
        input.ContainerPath.ShouldBe("dbo.Players");
    }

    [Fact]
    public void MembersThatAreNullOrEmptyAreNotSentSoAReadBackIsAccepted()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto
            {
                InputAlias = "players", InputKind = "DataSet", DataSetName = "AfcPlayers", ConnectionName = null, ContainerPath = string.Empty,
                ScalarValueType = null, ScalarSerializedValue = string.Empty
            }],
            NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    public void AnUnknownKindIsStillRefusedAsAnUnknownKind()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "x", InputKind = "NoSuchKind", DataSetName = "AfcPlayers" }], NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21536");
    }

    [Theory]
    [InlineData("DataSet", null, null, null, null, "dataSetName")]
    [InlineData("DataSet", "", null, null, null, "dataSetName")]
    [InlineData("DataSet", "  ", null, null, null, "dataSetName")]
    [InlineData("Container", null, null, "dbo.Players", null, "connectionName")]
    [InlineData("Container", null, "Main", null, null, "containerPath")]
    [InlineData("Container", null, "Main", "", null, "containerPath")]
    public void AnInputThatLeavesOutARequiredMemberIsRefusedNamingIt(
        string kind, string? dataSetName, string? connectionName, string? containerPath, string? scalarType, string member)
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto
            {
                InputAlias = "in", InputKind = kind, DataSetName = dataSetName, ConnectionName = connectionName, ContainerPath = containerPath,
                ScalarValueType = scalarType
            }],
            NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        var refusal = result.Messages[^1];
        refusal.Code.ShouldBe("ENDPOINTS-21548");
        var text = refusal.ShouldBeAssignableTo<IClientMessage>().ClientText;
        text.ShouldContain("'in'");
        text.ShouldContain(kind);
        text.ShouldContain($"'{member}'");
    }

    [Theory]
    [InlineData(null, "5", "scalarValueType")]
    [InlineData("Int32", null, "scalarSerializedValue")]
    public void AScalarInputThatLeavesOutAMemberIsRefusedNamingIt(string? valueType, string? serialized, string member)
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "n", InputKind = "Scalar", ScalarValueType = valueType, ScalarSerializedValue = serialized }],
            NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21548");
        result.Messages[^1].ShouldBeAssignableTo<IClientMessage>().ClientText.ShouldContain($"'{member}'");
    }

    [Fact]
    public void AScalarInputWithBothMembersIsMapped()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "n", InputKind = "Scalar", ScalarValueType = "Int32", ScalarSerializedValue = "5" }],
            NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldHaveSingleItem().ScalarValue!.SerializedValue.ShouldBe("5");
    }

    [Fact]
    public void AScalarInputWithAnUnknownValueTypeIsStillRefusedAsAnUnknownType()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "n", InputKind = "Scalar", ScalarValueType = "NoSuchType", ScalarSerializedValue = "5" }],
            NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21537");
    }

    [Fact]
    public void AnEmptyStringScalarValueIsAValueAndIsMapped()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "s", InputKind = "Scalar", ScalarValueType = "String", ScalarSerializedValue = string.Empty }],
            NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldHaveSingleItem().ScalarValue!.SerializedValue.ShouldBeEmpty();
    }

    [Fact]
    public void ABlankScalarValueTypeIsLeftOut()
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "s", InputKind = "Scalar", ScalarValueType = " ", ScalarSerializedValue = "x" }],
            NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21548");
        result.Messages[^1].ShouldBeAssignableTo<IClientMessage>().ClientText.ShouldContain("'scalarValueType'");
    }

    [Theory]
    [InlineData("Int32", "")]
    [InlineData("Int32", "abc")]
    [InlineData("Int32", "1.5")]
    [InlineData("Int32", "2147483648")]
    [InlineData("Int64", "")]
    [InlineData("Int64", "99999999999999999999999")]
    [InlineData("Decimal", "")]
    [InlineData("Decimal", "1.5.2")]
    [InlineData("Decimal", "twelve")]
    public void AScalarValueThatIsNotAValueOfItsTypeIsRefusedNamingTheInputAndTheType(string valueType, string value)
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "n", InputKind = "Scalar", ScalarValueType = valueType, ScalarSerializedValue = value }],
            NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        var refusal = result.Messages[^1];
        refusal.Code.ShouldBe("ENDPOINTS-21549");
        var text = refusal.ShouldBeAssignableTo<IClientMessage>().ClientText;
        text.ShouldContain("'n'");
        text.ShouldContain($"'{valueType}'");
    }

    [Theory]
    [InlineData("Int32", "5")]
    [InlineData("Int32", "-2147483648")]
    [InlineData("Int64", "9223372036854775807")]
    [InlineData("Decimal", "1.25")]
    [InlineData("Decimal", "-0.5")]
    [InlineData("String", "")]
    [InlineData("String", "Chiefs")]
    [InlineData("CharArray", "")]
    [InlineData("CharArray", "abc")]
    public void AScalarValueThatIsAValueOfItsTypeIsMapped(string valueType, string value)
    {
        var result = CalculationEntityInputMapper.ToInputs(
            [new CalculationEntityInputDto { InputAlias = "n", InputKind = "Scalar", ScalarValueType = valueType, ScalarSerializedValue = value }],
            NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldHaveSingleItem().ScalarValue!.SerializedValue.ShouldBe(value);
    }
}
