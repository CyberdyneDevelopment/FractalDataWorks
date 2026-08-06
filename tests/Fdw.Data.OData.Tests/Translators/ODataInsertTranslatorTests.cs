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

public sealed class ODataInsertTranslatorTests
{
    private readonly ODataInsertTranslator _sut = new();

    private static Mock<IStorageContainer> CreateContainer(string name = "Customers")
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([]);

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
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("ODataInsert");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForNullContainer()
    {
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = new { Name = "Test" } });
        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenDataMissing()
    {
        var container = CreateContainer();
        var command = CreateCommand();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenDataIsNull()
    {
        var container = CreateContainer();
        var metadata = new Dictionary<string, object>();
        // Data key exists but value is null - need to bypass the compiler null check
        metadata["Data"] = null!;
        var command = CreateCommand(metadata);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertReturnsPostRequest()
    {
        var container = CreateContainer();
        var entity = new { Name = "Acme", Email = "info@acme.com" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Method.ShouldBe(HttpMethod.Post);
        result.Value.RequestUri!.ToString().ShouldBe("/Customers");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertSetsJsonContent()
    {
        var container = CreateContainer();
        var entity = new { Name = "Acme" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Content.ShouldNotBeNull();
        result.Value.Content!.Headers.ContentType!.MediaType.ShouldBe("application/json");

        var body = await result.Value.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("Acme");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertHandlesContainerNameWithLeadingSlash()
    {
        var container = CreateContainer("/api/Customers");
        var entity = new { Name = "Acme" };
        var command = CreateCommand(new Dictionary<string, object> { ["Data"] = entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldStartWith("/api/Customers");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenMetadataIsNull()
    {
        var container = CreateContainer();
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns((IReadOnlyDictionary<string, object>?)null!);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }
}
