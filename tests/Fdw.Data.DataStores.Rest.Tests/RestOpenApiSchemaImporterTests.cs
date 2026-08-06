using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Shouldly;
using Moq;

namespace Fdw.Data.DataStores.Rest.Tests;

/// <summary>
/// Tests for <see cref="RestOpenApiSchemaImporter"/>.
/// </summary>
/// <remarks>
/// Why: unlike <see cref="ODataSchemaImporter"/>, this importer's <c>FetchSpec</c>/<c>Validate</c>
/// support a local file-path source in addition to HTTP (<c>Uri.TryCreate(source, UriKind.Absolute,
/// ...)</c> resolves an absolute Unix path to the "file" scheme, which is neither "http" nor
/// "https", so the importer falls through to <c>File.Exists</c>/<c>File.ReadAllTextAsync</c>).
/// That lets these tests exercise the ENTIRE public <see cref="RestOpenApiSchemaImporter.Import"/>
/// pipeline — spec fetch, OpenAPI parsing, endpoint/field/format extraction — against a real temp
/// file, with no network access and no mocking of the OpenAPI reader.
/// </remarks>
public sealed class RestOpenApiSchemaImporterTests : IDisposable
{
    private readonly string _tempDirectory = Directory.CreateTempSubdirectory("fdw-rest-openapi-tests-").FullName;

    private static RestOpenApiSchemaImporter CreateImporter() => new(Mock.Of<ILogger<RestOpenApiSchemaImporter>>());

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsArgumentNullExceptionWhenLoggerIsNull()
    {
        // Act
        var act = () => new RestOpenApiSchemaImporter(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsBaseTypeOptionIdentity()
    {
        // Act
        var importer = CreateImporter();

        // Assert
        importer.Id.ShouldBe(2);
        importer.Name.ShouldBe("OpenApi");
        importer.Description.ShouldBe("Imports schema from OpenAPI 3.0/Swagger specifications");
        importer.DataStoreType.ShouldBe("Rest");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ImportReturnsFailureWithSpecRequiredCodeWhenSourceIsNullOrWhitespace(string? source)
    {
        // Arrange
        var importer = CreateImporter();

        // Act
        var result = await importer.Import(source!, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("OpenApiSpecRequired");
        result.Code!.Code.ShouldBe("REST-21001");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateReturnsFailureWithSpecRequiredCodeWhenSourceIsNullOrWhitespace(string? source)
    {
        // Arrange
        var importer = CreateImporter();

        // Act
        var result = await importer.Validate(source!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("OpenApiSpecRequired");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateReturnsSuccessTrueWhenLocalFileExists()
    {
        // Arrange
        var importer = CreateImporter();
        var path = WriteTempFile("exists.json", "{}");

        // Act
        var result = await importer.Validate(path, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateReturnsSuccessFalseWhenLocalFileDoesNotExist()
    {
        // Arrange
        var importer = CreateImporter();
        var path = Path.Combine(_tempDirectory, "does-not-exist.json");

        // Act
        var result = await importer.Validate(path, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ImportReturnsFailureWithFileNotFoundCodeWhenLocalFileDoesNotExist()
    {
        // Arrange
        var importer = CreateImporter();
        var path = Path.Combine(_tempDirectory, "missing-spec.json");

        // Act
        var result = await importer.Import(path, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("OpenApiFileNotFound");
        result.Code!.Code.ShouldBe("REST-30000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ImportReturnsFailureWhenSpecContentIsNotValidOpenApi()
    {
        // Arrange
        var importer = CreateImporter();
        var path = WriteTempFile("garbage.json", "{ this is not a valid OpenAPI document");

        // Act
        var result = await importer.Import(path, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeNull();
        result.Messages.ShouldNotBeEmpty();
        result.Messages[0].Code.ShouldBe("REST-91001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ImportParsesEndpointsFieldsAndFormatFromLocalOpenApiSpec()
    {
        // Arrange: a minimal but realistic OpenAPI 3.0 document with two operations under one
        // path — exercises the full offline (no-HTTP) import pipeline: spec fetch (file), OpenAPI
        // parsing, per-operation endpoint/path creation, request/response/parameter field mapping,
        // and JSON-vs-XML format detection.
        var importer = CreateImporter();
        var path = WriteTempFile("petstore.json", PetStoreOpenApiJson);

        // Act
        var result = await importer.Import(path, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataStore = result.Value!;
        dataStore.Name.ShouldBe("Test Pet Store");
        dataStore.ServiceType.ShouldBe("DataStore");
        dataStore.ServiceOptionType.ShouldBe("Rest");
        dataStore.Paths.Count.ShouldBe(2);
        dataStore.Paths.Select(p => p.Name).ShouldBe(["listPets", "createPet"], ignoreOrder: true);
        dataStore.Paths.ShouldAllBe(p => p.Path == "http://api.example.com/pets");
        dataStore.Paths.ShouldAllBe(p => p.PathType == "HttpPath");
        dataStore.Paths.ShouldAllBe(p => p.Containers.Count == 1);
        dataStore.Paths.ShouldAllBe(p => p.Containers[0].TypeId == "Endpoint");
        dataStore.Paths.ShouldAllBe(p => p.Containers[0].Format == "Json");

        var listPets = dataStore.Paths.Single(p => p.Name == "listPets").Containers[0];
        listPets.Fields.Select(f => f.Name).ShouldBe(["Response.id", "Response.name", "limit"], ignoreOrder: true);
        listPets.Fields.Single(f => f.Name == "Response.id").DataType.ShouldBe("Int64");
        listPets.Fields.Single(f => f.Name == "Response.id").IsNullable.ShouldBeFalse();
        listPets.Fields.Single(f => f.Name == "Response.name").DataType.ShouldBe("String");
        listPets.Fields.Single(f => f.Name == "Response.name").IsNullable.ShouldBeTrue();
        listPets.Fields.Single(f => f.Name == "limit").DataType.ShouldBe("Int32");

        var createPet = dataStore.Paths.Single(p => p.Name == "createPet").Containers[0];
        createPet.Fields.Select(f => f.Name).ShouldBe(["Request.name", "Response.id"], ignoreOrder: true);
        createPet.Fields.Single(f => f.Name == "Request.name").IsNullable.ShouldBeFalse();
        createPet.Fields.Single(f => f.Name == "Response.id").IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ImportExcludesEndpointsNotMatchingIncludeSchemasOption()
    {
        // Arrange
        var importer = CreateImporter();
        var path = WriteTempFile("petstore-filtered.json", PetStoreOpenApiJson);
        var options = new Fdw.Data.SchemaImporters.Abstractions.Configuration.SchemaImporterOptions
        {
            IncludeSchemas = ["POST"]
        };

        // Act
        var result = await importer.Import(path, options, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Paths.Count.ShouldBe(1);
        result.Value!.Paths[0].Name.ShouldBe("createPet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ImportHonorsMaxContainersOption()
    {
        // Arrange
        var importer = CreateImporter();
        var path = WriteTempFile("petstore-maxcontainers.json", PetStoreOpenApiJson);
        var options = new Fdw.Data.SchemaImporters.Abstractions.Configuration.SchemaImporterOptions
        {
            MaxContainers = 1
        };

        // Act
        var result = await importer.Import(path, options, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Paths.Count.ShouldBe(1);
    }

    private string WriteTempFile(string fileName, string content)
    {
        var path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private const string PetStoreOpenApiJson = """
    {
      "openapi": "3.0.0",
      "info": { "title": "Test Pet Store", "version": "1.0.0" },
      "servers": [ { "url": "http://api.example.com" } ],
      "paths": {
        "/pets": {
          "get": {
            "operationId": "listPets",
            "parameters": [
              { "name": "limit", "in": "query", "required": false, "schema": { "type": "integer", "format": "int32" } }
            ],
            "responses": {
              "200": {
                "description": "A list of pets",
                "content": {
                  "application/json": {
                    "schema": {
                      "type": "object",
                      "required": ["id"],
                      "properties": {
                        "id": { "type": "integer", "format": "int64" },
                        "name": { "type": "string" }
                      }
                    }
                  }
                }
              }
            }
          },
          "post": {
            "operationId": "createPet",
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "required": ["name"],
                    "properties": {
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            },
            "responses": {
              "200": {
                "description": "Created",
                "content": {
                  "application/json": {
                    "schema": {
                      "type": "object",
                      "properties": {
                        "id": { "type": "integer", "format": "int64" }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
    """;
}
