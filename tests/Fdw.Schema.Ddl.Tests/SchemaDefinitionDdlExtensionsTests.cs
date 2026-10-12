using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Schema.Ddl;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Extensions;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Ddl.Tests;

public sealed class SchemaDefinitionDdlExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDdlCommandsDelegatesToGenerator()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();
        var commands = new List<IDdlCommand> { new CreateSchemaCommand { Name = "dbo" } };

        generator
            .Setup(g => g.GenerateCommands(schema.Object, null))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Success(commands));

        var result = schema.Object.ToDdlCommands(generator.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDdlCommandsPassesOptions()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();
        var options = new DdlGenerationOptions();
        var commands = new List<IDdlCommand>();

        generator
            .Setup(g => g.GenerateCommands(schema.Object, options))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Success(commands));

        var result = schema.Object.ToDdlCommands(generator.Object, options);

        generator.Verify(g => g.GenerateCommands(schema.Object, options), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDdlCommandsReturnsFailureFromGenerator()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();

        generator
            .Setup(g => g.GenerateCommands(schema.Object, null))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Failure(
                Results.DdlResultCodes.ByName("CommandGenerationFailed")));

        var result = schema.Object.ToDdlCommands(generator.Object);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToSqlScriptGeneratesScriptFromCommands()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();
        var commands = new List<IDdlCommand> { new CreateSchemaCommand { Name = "dbo" } };

        generator
            .Setup(g => g.GenerateCommands(schema.Object, null))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Success(commands));

        generator
            .Setup(g => g.GenerateScript(It.IsAny<IReadOnlyList<IDdlCommand>>()))
            .Returns(GenericResult<string>.Success("CREATE SCHEMA dbo;"));

        var result = schema.Object.ToSqlScript(generator.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("CREATE SCHEMA dbo;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToSqlScriptReturnsFailureWhenCommandGenerationFails()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();

        generator
            .Setup(g => g.GenerateCommands(schema.Object, null))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Failure(
                Results.DdlResultCodes.ByName("CommandGenerationFailed")));

        var result = schema.Object.ToSqlScript(generator.Object);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToSqlScriptReturnsFailureWhenCommandGenerationFailsWithErrorMessages()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();

        generator
            .Setup(g => g.GenerateCommands(schema.Object, null))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Failure(
                Results.DdlResultCodes.ByName("CommandGenerationFailed")));

        var result = schema.Object.ToSqlScript(generator.Object);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToSqlScriptReturnsNoCommandsGeneratedWhenValueIsNull()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();

        generator
            .Setup(g => g.GenerateCommands(schema.Object, null))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Success(null!));

        var result = schema.Object.ToSqlScript(generator.Object);

        result.Messages.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToSqlScriptReturnsNoCommandsGeneratedWhenEmpty()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();

        generator
            .Setup(g => g.GenerateCommands(schema.Object, null))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Success(new List<IDdlCommand>()));

        var result = schema.Object.ToSqlScript(generator.Object);

        result.Messages.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToSqlScriptPassesOptionsToGenerator()
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        var generator = new Mock<IDdlGenerator>();
        var options = new DdlGenerationOptions();
        var commands = new List<IDdlCommand> { new CreateSchemaCommand { Name = "dbo" } };

        generator
            .Setup(g => g.GenerateCommands(schema.Object, options))
            .Returns(GenericResult<IReadOnlyList<IDdlCommand>>.Success(commands));

        generator
            .Setup(g => g.GenerateScript(It.IsAny<IReadOnlyList<IDdlCommand>>()))
            .Returns(GenericResult<string>.Success("script"));

        var result = schema.Object.ToSqlScript(generator.Object, options);

        generator.Verify(g => g.GenerateCommands(schema.Object, options), Times.Once);
    }
}
