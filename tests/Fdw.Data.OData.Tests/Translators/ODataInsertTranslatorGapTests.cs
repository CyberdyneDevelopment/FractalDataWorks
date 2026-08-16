using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.OData;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.OData.Tests.Translators;

/// <summary>
/// Gap tests for ODataInsertTranslator - covers: complex entity serialization,
/// nested object in data, multiple properties.
/// </summary>
public sealed class ODataInsertTranslatorGapTests
{
    private readonly ODataInsertTranslator _sut = new();

    private static Mock<IStorageContainer> CreateContainer(string name = "Customers")
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([]);
        schema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container;
    }

    private static Mock<IDataCommand> CreateCommand(Dictionary<string, object>? metadata = null)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(
            metadata != null
                ? new Dictionary<string, object>(metadata, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        return command;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertSerializesComplexEntity()
    {
        // Arrange
        var container = CreateContainer();
        var entity = new
        {
            Name = "Acme Corp",
            Email = "contact@acme.com",
            IsActive = true,
            Rating = 4.5
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var body = await result.Value!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Acme Corp");
        body.ShouldContain("contact@acme.com");
        body.ShouldContain("true");
        body.ShouldContain("4.5");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertWithContainerNameWithoutSlash()
    {
        // Arrange - name without leading slash
        var container = CreateContainer("Products");
        var entity = new { Name = "Widget" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Products");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertSetsPostMethod()
    {
        // Arrange
        var container = CreateContainer();
        var entity = new { Name = "Test" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertSetsUtf8Encoding()
    {
        // Arrange
        var container = CreateContainer();
        var entity = new { Name = "Test" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Content!.Headers.ContentType!.CharSet.ShouldBe("utf-8");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertHandlesDictionaryData()
    {
        // Arrange - Data is a dictionary (serialized as JSON object)
        var container = CreateContainer();
        var data = new Dictionary<string, object>
        {
            ["Name"] = "FromDict",
            ["Value"] = 42
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = data });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var body = await result.Value!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("FromDict");
        body.ShouldContain("42");
    }
}
