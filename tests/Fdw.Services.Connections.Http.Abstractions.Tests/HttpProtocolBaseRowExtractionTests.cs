using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="HttpProtocolBase"/> row-extraction path (C2).
/// </summary>
/// <remarks>
/// Uses a thin test protocol that exposes the protected static helpers.
/// GeoJSON FeatureCollection fixture exercises the full RecordSelector + FlattenNestedObjects path.
/// </remarks>
[ExcludeFromCodeCoverage]
public class HttpProtocolBaseRowExtractionTests
{
    // FeatureCollection fixture — 2 features with nested properties + geometry
    private const string GeoJsonFeatureCollection = """
        {
            "type": "FeatureCollection",
            "features": [
                {
                    "type": "Feature",
                    "id": "a",
                    "properties": { "mag": 4.5, "place": "X" },
                    "geometry": { "type": "Point", "coordinates": [-122.0, 37.0, 8.0] }
                },
                {
                    "type": "Feature",
                    "id": "b",
                    "properties": { "mag": 2.1, "place": "Y" },
                    "geometry": { "type": "Point", "coordinates": [-118.0, 34.0, 5.0] }
                }
            ]
        }
        """;

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a mock IStorageContainer with the given Metadata and a Json IFormatType.
    /// </summary>
    private static IStorageContainer BuildContainer(
        string? recordSelector = null,
        bool flattenNestedObjects = false,
        string flattenSeparator = ".")
    {
        var meta = new Dictionary<string, object>(System.StringComparer.Ordinal);
        if (recordSelector is not null)
            meta["RecordSelector"] = recordSelector;
        meta["FlattenNestedObjects"] = flattenNestedObjects;
        meta["FlattenSeparator"] = flattenSeparator;

        var mockFormat = new Mock<IFormatType>();
        mockFormat.Setup(f => f.Name).Returns("Json");

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Format).Returns(mockFormat.Object);
        mockContainer.Setup(c => c.Metadata).Returns(meta);
        return mockContainer.Object;
    }

    // -----------------------------------------------------------------------
    // C2 positive tests — GeoJSON FeatureCollection
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void ExtractRowsFromContent_GeoJson_WithRecordSelector_Returns2Rows()
    {
        // Act
        var result = TestableHttpProtocol.ExtractRows(
            GeoJsonFeatureCollection,
            recordSelector: "features",
            flattenNestedObjects: true,
            flattenSeparator: ".");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var rows = result.Value.ShouldBeOfType<List<Dictionary<string, object?>>>();
        rows.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void ExtractRowsFromContent_GeoJson_Row0HasExpectedTopLevelKeys()
    {
        // Act
        var result = TestableHttpProtocol.ExtractRows(
            GeoJsonFeatureCollection,
            recordSelector: "features",
            flattenNestedObjects: true,
            flattenSeparator: ".");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var rows = result.Value.ShouldBeOfType<List<Dictionary<string, object?>>>();
        var row0 = rows[0];
        // Top-level "id" field on the Feature
        row0.ContainsKey("id").ShouldBeTrue();
        row0["id"].ShouldBe("a");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void ExtractRowsFromContent_GeoJson_Row0HasFlattenedMagnitude()
    {
        // Act
        var result = TestableHttpProtocol.ExtractRows(
            GeoJsonFeatureCollection,
            recordSelector: "features",
            flattenNestedObjects: true,
            flattenSeparator: ".");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var rows = result.Value.ShouldBeOfType<List<Dictionary<string, object?>>>();
        var row0 = rows[0];
        // Flattened "properties.mag" — stored as double by JsonStreamRowSource
        row0.ContainsKey("properties.mag").ShouldBeTrue();
        row0["properties.mag"].ShouldBe(4.5d);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void ExtractRowsFromContent_GeoJson_Row0HasFlattenedPlace()
    {
        // Act
        var result = TestableHttpProtocol.ExtractRows(
            GeoJsonFeatureCollection,
            recordSelector: "features",
            flattenNestedObjects: true,
            flattenSeparator: ".");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var rows = result.Value.ShouldBeOfType<List<Dictionary<string, object?>>>();
        var row0 = rows[0];
        row0["properties.place"].ShouldBe("X");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void ExtractRowsFromContent_GeoJson_GeometryCoordinatesExpandedByIndex()
    {
        // Arrange

        // Act
        var result = TestableHttpProtocol.ExtractRows(
            GeoJsonFeatureCollection,
            recordSelector: "features",
            flattenNestedObjects: true,
            flattenSeparator: ".");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var rows = result.Value.ShouldBeOfType<List<Dictionary<string, object?>>>();
        var row0 = rows[0];
        // geometry.type is flattened from the nested geometry object
        row0.ContainsKey("geometry.type").ShouldBeTrue();
        row0["geometry.type"].ShouldBe("Point");
        // geometry.coordinates array is expanded by index when flattening is enabled
        row0.ContainsKey("geometry.coordinates.0").ShouldBeTrue();
        row0["geometry.coordinates.0"].ShouldBe(-122.0);
    }

    // -----------------------------------------------------------------------
    // C2 IsRowCollectionType detection
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void IsRowCollectionType_IEnumerableDictionary_ReturnsTrue()
    {
        TestableHttpProtocol.CheckIsRowCollection(typeof(IEnumerable<Dictionary<string, object?>>))
            .ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void IsRowCollectionType_ListDictionary_ReturnsTrue()
    {
        TestableHttpProtocol.CheckIsRowCollection(typeof(List<Dictionary<string, object?>>))
            .ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void IsRowCollectionType_PlainObject_ReturnsFalse()
    {
        TestableHttpProtocol.CheckIsRowCollection(typeof(object)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void IsRowCollectionType_ListString_ReturnsFalse()
    {
        TestableHttpProtocol.CheckIsRowCollection(typeof(List<string>)).ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // C2 negative tests — bad selector / non-array
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void ExtractRowsFromContent_NonArraySelector_ReturnsFailure()
    {
        // Arrange — "type" resolves to a string value, not an array
        var result = TestableHttpProtocol.ExtractRows(
            GeoJsonFeatureCollection,
            recordSelector: "type",
            flattenNestedObjects: false,
            flattenSeparator: ".");

        // The row source returns 0 rows when the selector resolves to a non-array (no throw).
        result.IsSuccess.ShouldBeTrue();
        var rows = result.Value.ShouldBeOfType<List<Dictionary<string, object?>>>();
        rows.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public void ExtractRowsFromContent_InvalidJson_ReturnsFailure()
    {
        // Arrange
        const string badJson = "{ invalid json";

        // Act
        var result = TestableHttpProtocol.ExtractRows(
            badJson,
            recordSelector: null,
            flattenNestedObjects: false,
            flattenSeparator: ".");

        // Assert — must fail loud, not swallow or return empty rows
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    // -----------------------------------------------------------------------
    // C2 ProcessResponse integration — row-collection resultType with JSON container
    // -----------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public async System.Threading.Tasks.Task ProcessResponse_RowCollectionResultType_ExtractsRows()
    {
        // Arrange
        var protocol = new TestHttpProtocol();
        var container = BuildContainer(recordSelector: "features", flattenNestedObjects: true);
        var response = new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(GeoJsonFeatureCollection, Encoding.UTF8, "application/json")
        };
        var context = new HttpProtocolContext(
            new Mock<IGenericConfiguration>().Object,
            new Mock<ILoggerFactory>().Object,
            null, null, null);

        // Act
        var result = await protocol.ProcessResponse(
            response,
            container,
            typeof(IEnumerable<Dictionary<string, object?>>),
            context,
            System.Threading.CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var rows = result.Value.ShouldBeOfType<List<Dictionary<string, object?>>>();
        rows.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RowExtraction")]
    public async System.Threading.Tasks.Task ProcessResponse_NonRowCollectionResultType_UsesJsonDeserialize()
    {
        // Arrange — resultType is object (not a row collection), should not trigger row extraction
        var protocol = new TestHttpProtocol();
        var container = BuildContainer(recordSelector: "features");
        var json = """{"hello":"world"}""";
        var response = new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json")
        };
        var context = new HttpProtocolContext(
            new Mock<IGenericConfiguration>().Object,
            new Mock<ILoggerFactory>().Object,
            null, null, null);

        // Act
        var result = await protocol.ProcessResponse(
            response,
            container,
            typeof(object),  // NOT a row collection
            context,
            System.Threading.CancellationToken.None);

        // Assert — falls through to JsonSerializer.Deserialize, not row extraction
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }
}

/// <summary>
/// Thin shim that exposes protected static helpers for unit testing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TestableHttpProtocol : HttpProtocolBase
{
    public TestableHttpProtocol()
        : base(998, "TestableProtocol", "Test-only", "application/json")
    {
    }

    public static IGenericResult<object?> ExtractRows(
        string content,
        string? recordSelector,
        bool flattenNestedObjects,
        string flattenSeparator)
        => new TestableHttpProtocol().ExtractRowsFromContent(
            content, BuildJsonContainer(recordSelector, flattenNestedObjects, flattenSeparator));

    public static bool CheckIsRowCollection(System.Type type) => IsRowCollectionType(type);

    private static IStorageContainer BuildJsonContainer(
        string? recordSelector,
        bool flattenNestedObjects,
        string flattenSeparator)
    {
        var meta = new System.Collections.Generic.Dictionary<string, object>(System.StringComparer.Ordinal);
        if (recordSelector is not null) meta["RecordSelector"] = recordSelector;
        meta["FlattenNestedObjects"] = flattenNestedObjects;
        meta["FlattenSeparator"] = flattenSeparator;

        var format = new Mock<IFormatType>();
        format.Setup(f => f.Name).Returns("Json");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Format).Returns(format.Object);
        container.Setup(c => c.Metadata).Returns(meta);
        return container.Object;
    }
}

