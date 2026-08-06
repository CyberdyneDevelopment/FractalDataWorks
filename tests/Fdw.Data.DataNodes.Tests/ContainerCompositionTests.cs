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
        // Why: a container can expose a non-default format on any transport (e.g. Xml over Http).
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

        // Why: an explicit-but-unknown format discriminator is a misconfiguration — it resolves to the
        // NotFound sentinel (observable as a failed read), never a guessed Tabular/Json substitute.
        format.Name.ShouldBe(FormatTypes.NotFound.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResolveFormatNoFormatNotFoundDefaultReturnsNotFoundNoSilentFallback()
    {
        var cfg = new DataContainerConfiguration { Name = "Orphan" };

        var format = ContainerComposition.ResolveFormat(cfg, FormatTypes.NotFound);

        // Why: with no Format set and a transport that declares no DefaultResponseFormat (arriving here
        // as the NotFound sentinel), the result is NotFound — the no-fallback rule. The old node
        // hardcoded Tabular here.
        format.Name.ShouldBe(FormatTypes.NotFound.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildMetadataPopulatesResponseShapingKeys()
    {
        var cfg = WithJsonFormat("Quakes", recordSelector: "features", flatten: true, flattenSeparator: "_");

        var meta = ContainerComposition.BuildMetadata(cfg);

        meta["RecordSelector"].ShouldBe("features");
        meta["FlattenNestedObjects"].ShouldBe(true);
        meta["FlattenSeparator"].ShouldBe("_");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildMetadataOmitsUnsetKeys()
    {
        var meta = ContainerComposition.BuildMetadata(new DataContainerConfiguration { Name = "Empty" });

        meta.ContainsKey("RecordSelector").ShouldBeFalse();
        meta.ContainsKey("FlattenNestedObjects").ShouldBeFalse();
        meta.ContainsKey("FlattenSeparator").ShouldBeFalse();
    }
}
