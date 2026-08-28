using System;
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

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlUpdateTranslatorTests
{
    private readonly MsSqlUpdateTranslator _sut = new();

    private static Mock<IField> CreateField(
        string name,
        bool isIdentity = false,
        bool isComputed = false)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.IsComputed).Returns(isComputed);
        return field;
    }

    private static Mock<IStorageContainer> CreateContainer(
        string name = "Customers",
        IField[]? fields = null,
        string? primaryKeyFieldName = null)
    {
        var dbPath = new DatabasePath("", "dbo", name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields ?? []);
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields ?? []);

        var metadata = new Dictionary<string, object>();
        if (primaryKeyFieldName != null)
            metadata["SurrogateKeyField"] = primaryKeyFieldName;

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.Metadata).Returns(metadata);

        return container;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateWithFilterGeneratesCorrectSql()
    {
        var fields = new[]
        {
            CreateField("Id").Object,
            CreateField("Name").Object,
            CreateField("Email").Object
        };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "Id");

        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Id",
                Operator = new EqualOperator(),
                Value = 42
            }
        };

        var entity = new { Name = "Updated", Email = "new@test.com" };

        // Create a command that is both IFilterableCommand and IDataCommandWithInput
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("UPDATE [dbo].[Customers] SET");
        result.Value.CommandText.ShouldContain("[Name] = @set_Name");
        result.Value.CommandText.ShouldContain("[Email] = @set_Email");
        result.Value.CommandText.ShouldNotContain("[Id] = @set_Id");
        result.Value.CommandText.ShouldContain("WHERE [Id] = @where_p0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateExcludesPrimaryKeyFromSetClause()
    {
        var fields = new[]
        {
            CreateField("Id").Object,
            CreateField("Name").Object
        };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "Id");

        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 }
        };

        var entity = new { Name = "Test" };
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("SET [Id]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateExcludesIdentityFromSetClause()
    {
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Name").Object
        };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "Id");

        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 }
        };

        var entity = new { Name = "Test" };
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("SET [Id]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateReturnsFailureForNullContainer()
    {
        var command = new Mock<IFilterableCommand>();
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(new { Name = "Test" });

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateReturnsFailureForNonDatabasePath()
    {
        var mockPath = new Mock<IPath>();
        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns([]);
        mockSchema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns(mockPath.Object);
        container.Setup(c => c.Schema).Returns(mockSchema.Object);

        var command = new Mock<IFilterableCommand>();
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(new { Name = "Test" });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUpdateReturnsFailureWhenNoInputData()
    {
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBaseOverloadReturnsFailureForNonFilterableCommand()
    {
        var container = CreateContainer();
        var genericCommand = new Mock<IDataCommand>();

        var result = await _sut.Translate(genericCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBaseOverloadDispatchesFilterableCommand()
    {
        var fields = new[]
        {
            CreateField("Id").Object,
            CreateField("Name").Object
        };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "Id");

        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 }
        };

        var entity = new { Name = "Test" };
        var filterableCommand = new Mock<IFilterableCommand>();
        filterableCommand.Setup(c => c.Filter).Returns(filter);
        filterableCommand.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(entity);

        // Cast to IDataCommand to hit the base overload, which should dispatch to the typed overload
        IDataCommand dataCommand = filterableCommand.Object;
        var result = await _sut.Translate(dataCommand, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("UPDATE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Update");
    }
}
