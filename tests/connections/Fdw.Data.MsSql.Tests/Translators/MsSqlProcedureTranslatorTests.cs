using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Data.MsSql.Translators;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Results;
using Microsoft.Data.SqlClient;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// FDW-6682: a host runs a named stored procedure of its SQL Server store through the gateway. <see cref="ExecuteProcedureCommand{T}"/> carries scalar parameters
/// and is translated to a stored procedure call whose every value is a bound parameter and never SQL text; a StoredProcedure container states Execute and nothing else.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlProcedureTranslatorTests
{
    private sealed record Claimed(string State);

    private readonly MsSqlProcedureTranslator _sut = new();

    private static Mock<IDataContainer> Procedure(string schema = "etl", string name = "ClaimPipelineRun", params string[] operations)
    {
        var container = TestContainers.Create(schema, name, "StoredProcedure");
        var stated = operations.Length == 0 ? new[] { "Execute" } : operations;
        container.Setup(c => c.SupportedOperations).Returns(stated);
        container.As<IStorageContainer>().Setup(c => c.SupportedOperations).Returns(stated);
        return container;
    }

    private static ExecuteProcedureCommand<Claimed> Call(params (string Name, object? Value)[] parameters)
        => new() { Parameters = parameters.ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal) };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AStoredProcedureContainerStatesExecuteAndNothingElse()
    {
        new StoredProcedureContainerType().SupportedOperations.ShouldBe(["Execute"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ACallIsAStoredProcedureCommandNamedByTheContainersAddressWithEveryValueAParameter()
    {
        var call = Call(("Keys", "[{\"KeyType\":\"WriteKey\"}]"), ("LeaseSeconds", 60), ("Cap", null));

        var result = await _sut.Translate(call, Procedure().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var native = result.Value.ShouldNotBeNull();
        native.CommandType.ShouldBe(CommandType.StoredProcedure);
        native.CommandText.ShouldBe("[etl].[ClaimPipelineRun]");
        native.Parameters["@Keys"].Value.ShouldBe("[{\"KeyType\":\"WriteKey\"}]");
        native.Parameters["@LeaseSeconds"].Value.ShouldBe(60);
        native.Parameters["@Cap"].Value.ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AParameterThatHoldsSqlTextIsOnlyAValue()
    {
        var call = Call(("Keys", "x'; DROP TABLE etl.PipelineRunClaim; --"));

        var result = await _sut.Translate(call, Procedure().Object, TestContext.Current.CancellationToken);

        var native = result.Value.ShouldNotBeNull();
        native.CommandText.ShouldBe("[etl].[ClaimPipelineRun]");
        native.CommandText.ShouldNotContain("DROP");
        native.Parameters["@Keys"].Value.ShouldBe("x'; DROP TABLE etl.PipelineRunClaim; --");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    [InlineData("a text", typeof(string))]
    [InlineData(true, typeof(bool))]
    [InlineData(7, typeof(int))]
    [InlineData(7L, typeof(long))]
    [InlineData(1.5, typeof(double))]
    public async Task EachScalarTypeTheCommandListsIsAccepted(object value, Type expected)
    {
        var result = await _sut.Translate(Call(("P", value)), Procedure().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Parameters["@P"].Value.ShouldBeOfType(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AGuidAndADateAreAccepted()
    {
        var when = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

        var result = await _sut.Translate(Call(("Id", Guid.NewGuid()), ("At", when)), Procedure().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    [InlineData("list")]
    [InlineData("object")]
    [InlineData("array")]
    public async Task AnyOtherTypeIsRefusedWithACodedMessageAndNeverWrittenAsText(string kind)
    {
        object value = kind switch
        {
            "list" => new List<string> { "a" },
            "array" => new[] { 1, 2 },
            _ => new object(),
        };

        var result = await _sut.Translate(Call(("P", value)), Procedure().Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldContain("MSSQL-60001");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("P");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    [InlineData("a b")]
    [InlineData("a;b")]
    [InlineData("a]")]
    [InlineData("")]
    public async Task AParameterNameThatIsNotAnIdentifierIsRefused(string name)
    {
        var result = await _sut.Translate(Call((name, 1)), Procedure().Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldContain("MSSQL-60002");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ACommandThatIsNotAnExecuteProcedureCommandIsRefused()
    {
        var other = new Mock<Fdw.Commands.Data.Abstractions.IDataCommand>();
        other.Setup(c => c.CommandType).Returns("Query");

        var result = await _sut.Translate(other.Object, Procedure().Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AContainerThatDoesNotStateExecuteIsRefusedByTheTranslatorItselfAndNothingIsBuilt()
    {
        var result = await _sut.Translate(Call(("Keys", "x")), Procedure(operations: "Query").Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldContain("DATA-61148");
    }
}
