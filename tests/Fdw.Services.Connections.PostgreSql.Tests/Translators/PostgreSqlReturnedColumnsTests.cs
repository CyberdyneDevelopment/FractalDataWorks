using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// An Insert or Update that names columns to return ends in a RETURNING clause; one that names none is the statement it
/// always was.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlReturnedColumnsTests
{
    private static Mock<IField> Field(string name, bool isIdentity = false)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        return field;
    }

    private static Mock<IStorageContainer> Container()
    {
        IField[] fields = [Field("id", isIdentity: true).Object, Field("name").Object, Field("email").Object];
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("customers");
        container.Setup(c => c.Path).Returns(new PostgreSqlDatabasePath(null, "public", "customers"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object> { ["SurrogateKeyField"] = "id" });
        return container;
    }

    private static IDataCommand InsertOf(params string[] returned)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[] { new { name = "Acme", email = "info@acme.test" } });
        command.As<IReturningCommand>().Setup(c => c.ReturnColumns).Returns(returned);
        return command.As<IDataCommand>().Object;
    }

    private static IFilterableCommand UpdateOf(params string[] returned)
    {
        var filter = new FilterExpression { Root = new FilterCondition { PropertyName = "id", Operator = new EqualOperator(), Value = 7 } };
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(new { name = "Renamed" });
        command.As<IReturningCommand>().Setup(c => c.ReturnColumns).Returns(returned);
        return command.Object;
    }

    [Fact]
    public async Task AnInsertThatNamesColumnsEndsInReturning()
    {
        var result = await new PostgreSqlInsertTranslator().Translate(InsertOf("id", "email"), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldContain("VALUES (@p0, @p1) RETURNING \"id\", \"email\";");
    }

    [Fact]
    public async Task AnInsertThatNamesNoColumnsHasNoReturning()
    {
        var result = await new PostgreSqlInsertTranslator().Translate(InsertOf(), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldNotContain("RETURNING");
    }

    [Fact]
    public async Task AnInsertThatNamesAColumnTheContainerDoesNotDeclareFailsNamingIt()
    {
        var result = await new PostgreSqlInsertTranslator().Translate(InsertOf("nope"), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.ToString()!.Contains("nope"));
    }

    [Fact]
    public async Task AnUpdateThatNamesColumnsEndsInReturningAfterWhere()
    {
        var result = await new PostgreSqlUpdateTranslator().Translate(UpdateOf("id", "name"), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain(" WHERE ");
        sql.ShouldEndWith(" RETURNING \"id\", \"name\"");
    }
}
