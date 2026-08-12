using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Shouldly;
using Moq;

namespace Fdw.Data.DataStores.Rest.Tests;

/// <summary>
/// Tests for <see cref="ODataSchemaImporter"/>.
/// </summary>
/// <remarks>
/// Why: <see cref="ODataSchemaImporter.Import"/> and <see cref="ODataSchemaImporter.Validate"/>
/// always reach out over HTTP (a bare <c>new HttpClient()</c> with no injection seam — the class
/// itself is <c>[ExcludeFromCodeCoverage]</c> for exactly this reason), so this suite exercises:
/// (1) the guard clauses that run before any HTTP call, and (2) the pure XML/EDM parsing helpers
/// via reflection, without ever touching the network. Reflection on private members is an
/// established pattern in this codebase's test suites for exercising pure logic that a
/// network-bound public method wraps.
/// </remarks>
public sealed class ODataSchemaImporterTests
{
    private static ODataSchemaImporter CreateImporter() => new(Mock.Of<ILogger<ODataSchemaImporter>>());

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsArgumentNullExceptionWhenLoggerIsNull()
    {
        // Act
        var act = () => new ODataSchemaImporter(null!);

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
        importer.Id.ShouldBe(3);
        importer.Name.ShouldBe("OData");
        importer.Description.ShouldBe("Imports schema from OData $metadata endpoints");
        importer.DataStoreType.ShouldBe("Rest");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async System.Threading.Tasks.Task ImportReturnsFailureWithServiceUrlRequiredCodeWhenSourceIsNullOrWhitespace(string? source)
    {
        // Arrange
        var importer = CreateImporter();

        // Act
        var result = await importer.Import(source!, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ODataServiceUrlRequired");
        result.Code!.Code.ShouldBe("REST-20000");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async System.Threading.Tasks.Task ValidateReturnsFailureWithServiceUrlRequiredCodeWhenSourceIsNullOrWhitespace(string? source)
    {
        // Arrange
        var importer = CreateImporter();

        // Act
        var result = await importer.Validate(source!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ODataServiceUrlRequired");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("http://host/odata", "http://host/odata", "http://host/odata/$metadata")]
    [InlineData("http://host/odata/", "http://host/odata", "http://host/odata/$metadata")]
    [InlineData("http://host/odata/$metadata", "http://host/odata", "http://host/odata/$metadata")]
    [InlineData("http://host/odata/$METADATA", "http://host/odata", "http://host/odata/$METADATA")]
    public void NormalizeMetadataUrlDerivesBaseAndMetadataUrls(string source, string expectedBaseUrl, string expectedMetadataUrl)
    {
        // Act
        var (baseUrl, metadataUrl) = InvokeNormalizeMetadataUrl(source);

        // Assert
        baseUrl.ShouldBe(expectedBaseUrl);
        metadataUrl.ShouldBe(expectedMetadataUrl);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DetectEdmNamespacePrefersV4WhenV4EntityTypesPresent()
    {
        // Arrange
        var edmxNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edmx";
        var edmV4Ns = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var doc = new XDocument(new XElement(edmxNs + "Edmx",
            new XElement(edmV4Ns + "EntityType", new XAttribute("Name", "Widget"))));

        // Act
        var detected = InvokeDetectEdmNamespace(doc);

        // Assert
        detected.ShouldBe(edmV4Ns);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DetectEdmNamespaceFallsBackToV3WhenOnlyV3EntityTypesPresent()
    {
        // Arrange
        var edmxNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edmx";
        var edmV3Ns = (XNamespace)"http://schemas.microsoft.com/ado/2009/11/edm";
        var doc = new XDocument(new XElement(edmxNs + "Edmx",
            new XElement(edmV3Ns + "EntityType", new XAttribute("Name", "Widget"))));

        // Act
        var detected = InvokeDetectEdmNamespace(doc);

        // Assert
        detected.ShouldBe(edmV3Ns);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void DetectEdmNamespaceDefaultsToV4WhenNoEntityTypesPresent()
    {
        // Arrange
        var edmxNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edmx";
        var edmV4Ns = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var doc = new XDocument(new XElement(edmxNs + "Edmx"));

        // Act
        var detected = InvokeDetectEdmNamespace(doc);

        // Assert
        detected.ShouldBe(edmV4Ns);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ShouldExcludeReturnsFalseWhenNoOptionsProvided()
    {
        // Act
        var excluded = InvokeShouldExclude("Widgets", null);

        // Assert
        excluded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ShouldExcludeReturnsTrueWhenNameDoesNotMatchIncludeSchemas()
    {
        // Arrange
        var options = new SchemaImporterOptions { IncludeSchemas = ["Sales"] };

        // Act
        var excluded = InvokeShouldExclude("Widgets", options);

        // Assert
        excluded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ShouldExcludeReturnsFalseWhenNameMatchesIncludeSchemas()
    {
        // Arrange
        var options = new SchemaImporterOptions { IncludeSchemas = ["Widget"] };

        // Act
        var excluded = InvokeShouldExclude("Widgets", options);

        // Assert
        excluded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ShouldExcludeReturnsTrueWhenNameMatchesExcludeSchemas()
    {
        // Arrange
        var options = new SchemaImporterOptions { ExcludeSchemas = ["Widget"] };

        // Act
        var excluded = InvokeShouldExclude("Widgets", options);

        // Assert
        excluded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ShouldExcludeReturnsFalseWhenNameDoesNotMatchExcludeSchemas()
    {
        // Arrange
        var options = new SchemaImporterOptions { ExcludeSchemas = ["Sales"] };

        // Act
        var excluded = InvokeShouldExclude("Widgets", options);

        // Assert
        excluded.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData("Edm.Int32", "Int32")]
    [InlineData("Edm.Boolean", "Boolean")]
    [InlineData("Int32", "Int32")]
    public void MapEdmTypeNameStripsEdmPrefixAndResolvesKnownTypes(string edmType, string expectedClrTypeName)
    {
        // Act
        var mapped = InvokeMapEdmTypeName(edmType);

        // Assert
        mapped.ShouldBe(expectedClrTypeName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapEdmTypeNameFallsBackToObjectForUnknownEdmType()
    {
        // Act
        var mapped = InvokeMapEdmTypeName("Edm.GeographyPoint");

        // Assert
        mapped.ShouldBe("Object");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ParseEdmxDocumentReturnsSuccessForValidXml()
    {
        // Arrange
        var importer = CreateImporter();
        const string xml = "<edmx:Edmx xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\" />";

        // Act
        var result = InvokeParseEdmxDocument(importer, xml);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Root!.Name.LocalName.ShouldBe("Edmx");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ParseEdmxDocumentReturnsFailureForMalformedXml()
    {
        // Arrange
        var importer = CreateImporter();
        const string malformedXml = "<edmx:Edmx this is not valid xml";

        // Act
        var result = InvokeParseEdmxDocument(importer, malformedXml);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeNull();
        result.Messages.ShouldNotBeEmpty();
        result.Messages[0].Code.ShouldBe("REST-91000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void BuildFieldsFromEntityTypeMapsPropertiesToFieldConfigurations()
    {
        // Arrange
        var edmNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var entityType = new XElement(edmNs + "EntityType",
            new XAttribute("Name", "Widget"),
            new XElement(edmNs + "Property", new XAttribute("Name", "Id"), new XAttribute("Type", "Edm.Int32"), new XAttribute("Nullable", "false")),
            new XElement(edmNs + "Property", new XAttribute("Name", "Description"), new XAttribute("Type", "Edm.String"), new XAttribute("Nullable", "true")));

        // Act
        var fields = InvokeBuildFieldsFromEntityType(entityType, edmNs);

        // Assert
        fields.Count.ShouldBe(2);
        fields[0].Name.ShouldBe("Id");
        fields[0].IsNullable.ShouldBeFalse();
        fields[1].Name.ShouldBe("Description");
        fields[1].IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void BuildFieldsFromEntityTypeSkipsPropertiesMissingNameOrType()
    {
        // Arrange
        var edmNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var entityType = new XElement(edmNs + "EntityType",
            new XAttribute("Name", "Widget"),
            new XElement(edmNs + "Property", new XAttribute("Type", "Edm.Int32")),
            new XElement(edmNs + "Property", new XAttribute("Name", "OnlyName")),
            new XElement(edmNs + "Property", new XAttribute("Name", "Valid"), new XAttribute("Type", "Edm.String")));

        // Act
        var fields = InvokeBuildFieldsFromEntityType(entityType, edmNs);

        // Assert
        fields.Count.ShouldBe(1);
        fields[0].Name.ShouldBe("Valid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ParseEntitySetsBuildsOneDataPathPerMatchingEntitySet()
    {
        // Arrange: a minimal but realistic EDMX v4 document with one EntityContainer/EntitySet
        // and its matching EntityType — exercises the full offline (no-HTTP) parsing pipeline that
        // ODataSchemaImporter.Import would otherwise only reach after a real network fetch.
        var edmNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var edmx = new XDocument(
            new XElement(edmNs + "Schema",
                new XElement(edmNs + "EntityType",
                    new XAttribute("Name", "Widget"),
                    new XElement(edmNs + "Property", new XAttribute("Name", "Id"), new XAttribute("Type", "Edm.Int32"), new XAttribute("Nullable", "false")),
                    new XElement(edmNs + "Property", new XAttribute("Name", "Name"), new XAttribute("Type", "Edm.String"), new XAttribute("Nullable", "true"))),
                new XElement(edmNs + "EntityContainer",
                    new XElement(edmNs + "EntitySet", new XAttribute("Name", "Widgets"), new XAttribute("EntityType", "Sample.Widget")))));

        var importer = CreateImporter();

        // Act
        var results = InvokeParseEntitySets(importer, edmx, edmNs, "http://host/odata", null);

        // Assert
        results.Count.ShouldBe(1);
        results[0].IsSuccess.ShouldBeTrue();
        var path = results[0].Value!;
        path.Name.ShouldBe("Widgets");
        path.PathName.ShouldBe("http://host/odata/Widgets");
        path.PathType.ShouldBe("HttpPath");
        path.Containers.Count.ShouldBe(1);
        path.Containers[0].TypeId.ShouldBe("Endpoint");
        path.Containers[0].Format.ShouldBe("Json");
        path.Containers[0].Fields.Count.ShouldBe(2);
        path.Containers[0].Fields.Select(f => f.Name).ShouldBe(["Id", "Name"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ParseEntitySetsSkipsEntitySetsWithoutMatchingEntityType()
    {
        // Arrange: an EntitySet references an EntityType that isn't defined anywhere in the document.
        var edmNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var edmx = new XDocument(
            new XElement(edmNs + "Schema",
                new XElement(edmNs + "EntityContainer",
                    new XElement(edmNs + "EntitySet", new XAttribute("Name", "Ghosts"), new XAttribute("EntityType", "Sample.Ghost")))));

        var importer = CreateImporter();

        // Act
        var results = InvokeParseEntitySets(importer, edmx, edmNs, "http://host/odata", null);

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ParseEntitySetsHonorsMaxContainersOption()
    {
        // Arrange
        var edmNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var edmx = new XDocument(
            new XElement(edmNs + "Schema",
                new XElement(edmNs + "EntityType", new XAttribute("Name", "Widget")),
                new XElement(edmNs + "EntityType", new XAttribute("Name", "Gadget")),
                new XElement(edmNs + "EntityContainer",
                    new XElement(edmNs + "EntitySet", new XAttribute("Name", "Widgets"), new XAttribute("EntityType", "Sample.Widget")),
                    new XElement(edmNs + "EntitySet", new XAttribute("Name", "Gadgets"), new XAttribute("EntityType", "Sample.Gadget")))));

        var importer = CreateImporter();
        var options = new SchemaImporterOptions { MaxContainers = 1 };

        // Act
        var results = InvokeParseEntitySets(importer, edmx, edmNs, "http://host/odata", options);

        // Assert
        results.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateEntitySetPathSucceedsWithEmptyFieldsWhenEntityTypeHasNoProperties()
    {
        // Arrange
        var importer = CreateImporter();
        var edmNs = (XNamespace)"http://docs.oasis-open.org/odata/ns/edm";
        var entityType = new XElement(edmNs + "EntityType", new XAttribute("Name", "Widget"));

        // Act
        var result = InvokeCreateEntitySetPath(importer, "http://host/odata", "Widgets", entityType, edmNs, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Containers[0].Fields.ShouldBeEmpty();
        result.Value!.PathName.ShouldBe("http://host/odata/Widgets");
    }

    private static (string BaseUrl, string MetadataUrl) InvokeNormalizeMetadataUrl(string source)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("NormalizeMetadataUrl", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("NormalizeMetadataUrl not found");
        return ((string, string))method.Invoke(null, [source])!;
    }

    private static XNamespace InvokeDetectEdmNamespace(XDocument edmx)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("DetectEdmNamespace", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("DetectEdmNamespace not found");
        return (XNamespace)method.Invoke(null, [edmx])!;
    }

    private static bool InvokeShouldExclude(string containerName, SchemaImporterOptions? options)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("ShouldExclude", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ShouldExclude not found");
        return (bool)method.Invoke(null, [containerName, options])!;
    }

    private static string InvokeMapEdmTypeName(string edmType)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("MapEdmTypeName", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("MapEdmTypeName not found");
        return (string)method.Invoke(null, [edmType])!;
    }

    private static Fdw.Results.IGenericResult<XDocument> InvokeParseEdmxDocument(ODataSchemaImporter importer, string metadataXml)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("ParseEdmxDocument", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ParseEdmxDocument not found");
        return (Fdw.Results.IGenericResult<XDocument>)method.Invoke(importer, [metadataXml])!;
    }

    private static List<Fdw.Services.Connections.DataContainerFieldConfiguration> InvokeBuildFieldsFromEntityType(XElement entityType, XNamespace edmNamespace)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("BuildFieldsFromEntityType", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildFieldsFromEntityType not found");
        return (List<Fdw.Services.Connections.DataContainerFieldConfiguration>)method.Invoke(null, [entityType, edmNamespace])!;
    }

    private static List<Fdw.Results.IGenericResult<Fdw.Services.Connections.DataPathConfiguration>> InvokeParseEntitySets(
        ODataSchemaImporter importer, XDocument edmx, XNamespace edmNamespace, string baseUrl, SchemaImporterOptions? options)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("ParseEntitySets", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ParseEntitySets not found");
        return (List<Fdw.Results.IGenericResult<Fdw.Services.Connections.DataPathConfiguration>>)method.Invoke(importer, [edmx, edmNamespace, baseUrl, options])!;
    }

    private static Fdw.Results.IGenericResult<Fdw.Services.Connections.DataPathConfiguration> InvokeCreateEntitySetPath(
        ODataSchemaImporter importer, string baseUrl, string entitySetName, XElement entityType, XNamespace edmNamespace, SchemaImporterOptions? options)
    {
        var method = typeof(ODataSchemaImporter).GetMethod("CreateEntitySetPath", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CreateEntitySetPath not found");
        return (Fdw.Results.IGenericResult<Fdw.Services.Connections.DataPathConfiguration>)method.Invoke(importer, [baseUrl, entitySetName, entityType, edmNamespace, options])!;
    }
}
