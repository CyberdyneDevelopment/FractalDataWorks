using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// An Insert or Update that names columns to return carries them in an OUTPUT clause; one that names none is the
/// statement it always was.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlReturnedColumnsTests
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
        IField[] fields = [Field("RowId", isIdentity: true).Object, Field("Name").Object, Field("Email").Object];
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Customers");
        container.Setup(c => c.Path).Returns(new DatabasePath("", "dbo", "Customers"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object> { ["SurrogateKeyField"] = "RowId" });
        return container;
    }

    private static IDataCommand InsertOf(params string[] returned)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[] { new { Name = "Acme", Email = "info@acme.test" } });
        command.As<IReturningCommand>().Setup(c => c.ReturnColumns).Returns(returned);
        return command.As<IDataCommand>().Object;
    }

    private static IFilterableCommand UpdateOf(params string[] returned)
    {
        var filter = new FilterExpression { Root = new FilterCondition { PropertyName = "RowId", Operator = new EqualOperator(), Value = 7 } };
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(new { Name = "Renamed" });
        command.As<IReturningCommand>().Setup(c => c.ReturnColumns).Returns(returned);
        return command.Object;
    }

    [Fact]
    public async Task AnInsertThatNamesColumnsReturnsThemFromOutputBeforeValuesAndSendsNoRowCount()
    {
        var result = await new MsSqlInsertTranslator().Translate(InsertOf("RowId", "Email"), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain("OUTPUT inserted.[RowId], inserted.[Email] VALUES");
        sql.ShouldNotContain("@@ROWCOUNT");
    }

    [Fact]
    public async Task AnInsertThatNamesNoColumnsIsTheStatementItAlwaysWas()
    {
        var result = await new MsSqlInsertTranslator().Translate(InsertOf(), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldNotContain("OUTPUT");
        result.Value.CommandText.ShouldContain("SELECT @@ROWCOUNT;");
    }

    [Fact]
    public async Task AnInsertThatNamesAColumnTheContainerDoesNotDeclareFailsNamingIt()
    {
        var result = await new MsSqlInsertTranslator().Translate(InsertOf("Nope"), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.ToString()!.Contains("Nope"));
    }

    [Fact]
    public async Task AnUpdateThatNamesColumnsReturnsThemFromOutputBeforeWhere()
    {
        var result = await new MsSqlUpdateTranslator().Translate(UpdateOf("RowId", "Name"), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain("OUTPUT inserted.[RowId], inserted.[Name] WHERE");
    }

    [Fact]
    public async Task AnUpdateThatNamesNoColumnsHasNoOutput()
    {
        var result = await new MsSqlUpdateTranslator().Translate(UpdateOf(), Container().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldNotContain("OUTPUT");
    }
}
