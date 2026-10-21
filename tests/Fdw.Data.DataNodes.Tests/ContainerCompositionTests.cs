using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.DataNodes;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// Pure in-memory tests for <see cref="ContainerComposition"/> — the per-container format +
/// response-shaping resolution that replaced the hardcoded <c>Format = Tabular</c> / empty-metadata
/// on the generic <c>DataContainer</c> node. Format + row-shaping are CONFIG-DRIVEN: read directly
/// from the container's own <see cref="DataContainerConfiguration"/> (its <c>Format</c> discriminator
/// + inline <c>RecordSelector</c>/<c>FlattenNestedObjects</c>/<c>FlattenSeparator</c> options).
/// The transport's <c>DefaultResponseFormat</c> is supplied by the caller (the transport's
/// <c>SupplyBuilder</c> resolves it at the transport boundary) — this package stays connection-agnostic.
/// </summary>
public sealed class ContainerCompositionTests
{
    private static DataContainerConfiguration WithJsonFormat(
        string name,
        string? recordSelector = null,
        bool? flatten = null,
        string? flattenSeparator = null)
        => new()
        {
            Name = name,
            Format = "Json",
            RecordSelector = recordSelector,
            FlattenNestedObjects = flatten,
            FlattenSeparator = flattenSeparator,
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResolveFormatExplicitFormatUsesIt()
    {
        var cfg = WithJsonFormat("Quakes");

        var format = ContainerComposition.ResolveFormat(cfg, FormatTypes.Tabular);

        format.Name.ShouldBe(FormatTypes.Json.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResolveFormatExplicitFormatOverridesTransportDefault()
    {
        var cfg = new DataContainerConfiguration { Name = "Feed", Format = "Xml" };

        var format = ContainerComposition.ResolveFormat(cfg, FormatTypes.Json);

        format.Name.ShouldBe(FormatTypes.Xml.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResolveFormatInvalidExplicitFormatReturnsNotFoundNoSilentFallback()
    {
        var cfg = new DataContainerConfiguration { Name = "Bad", Format = "NotARealFormat" };

        var format = ContainerComposition.ResolveFormat(cfg, FormatTypes.Json);

        format.Name.ShouldBe(FormatTypes.NotFound.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResolveFormatNoFormatNotFoundDefaultReturnsNotFoundNoSilentFallback()
    {
        var cfg = new DataContainerConfiguration { Name = "Orphan" };

        var format = ContainerComposition.ResolveFormat(cfg, FormatTypes.NotFound);

        format.Name.ShouldBe(FormatTypes.NotFound.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildMetadataPopulatesResponseShapingKeys()
    {
        var cfg = WithJsonFormat("Quakes", recordSelector: "features", flatten: true, flattenSeparator: "_");

        var meta = ContainerComposition.BuildMetadata(cfg, FormatTypes.Json);

        meta["RecordSelector"].ShouldBe("features");
        meta["FlattenNestedObjects"].ShouldBe(true);
        meta["FlattenSeparator"].ShouldBe("_");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildMetadataOmitsUnsetKeys()
    {
        var meta = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "Empty" }, FormatTypes.Json);

        meta.ContainsKey("RecordSelector").ShouldBeFalse();
        meta.ContainsKey("FlattenNestedObjects").ShouldBeFalse();
        meta.ContainsKey("FlattenSeparator").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildMetadataCarriesTheFileOptionsOnlyWhenTheyAreStated()
    {
        var stated = ContainerComposition.BuildMetadata(
            new DataContainerConfiguration { Name = "People", HasHeader = true, Separator = ";", SheetName = "Staff" }, FormatTypes.Delimited);
        var notHeader = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "Rows", HasHeader = false }, FormatTypes.Delimited);
        var none = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "Empty" }, FormatTypes.Delimited);

        stated["HasHeader"].ShouldBe(true);
        stated["Separator"].ShouldBe(";");
        stated["SheetName"].ShouldBe("Staff");
        notHeader["HasHeader"].ShouldBe(false);
        none.ContainsKey("HasHeader").ShouldBeFalse();
        none.ContainsKey("Separator").ShouldBeFalse();
        none.ContainsKey("SheetName").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BuildMetadataCarriesTheWriteTargetUnderTheKeysTheHttpWriterReadsOnlyWhenItIsStated()
    {
        var stated = ContainerComposition.BuildMetadata(
            new DataContainerConfiguration
            {
                Name = "BreweryOut",
                WriteEndpoint = "/api/v1/ingest",
                WriteMethod = "POST",
                WriteContentType = "application/json",
            },
            FormatTypes.Json);
        var onlyEndpoint = ContainerComposition.BuildMetadata(
            new DataContainerConfiguration { Name = "BreweryOut", WriteEndpoint = "/api/v1/ingest" }, FormatTypes.Json);
        var none = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "Empty" }, FormatTypes.Json);

        stated["Endpoint"].ShouldBe("/api/v1/ingest");
        stated["Method"].ShouldBe("POST");
        stated["ContentType"].ShouldBe("application/json");
        onlyEndpoint["Endpoint"].ShouldBe("/api/v1/ingest");
        onlyEndpoint.ContainsKey("Method").ShouldBeFalse();
        onlyEndpoint.ContainsKey("ContentType").ShouldBeFalse();
        none.ContainsKey("Endpoint").ShouldBeFalse();
        none.ContainsKey("Method").ShouldBeFalse();
        none.ContainsKey("ContentType").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BuildMetadataStatesTheSeparatorOnceFromTheFormatThatDefinesOne()
    {
        var csv = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "People" }, FormatTypes.Csv);
        var tsv = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "People" }, FormatTypes.Tsv);
        var delimited = ContainerComposition.BuildMetadata(
            new DataContainerConfiguration { Name = "People", Separator = "|" }, FormatTypes.Delimited);
        var noSeparator = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "People" }, FormatTypes.Delimited);

        csv["Separator"].ShouldBe(",");
        tsv["Separator"].ShouldBe("\t");
        delimited["Separator"].ShouldBe("|");
        noSeparator.ContainsKey("Separator").ShouldBeFalse();
    }
}
