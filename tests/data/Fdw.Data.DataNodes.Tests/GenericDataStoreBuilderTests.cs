using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// Tests for <see cref="GenericDataStoreBuilder"/> — specifically the container-format resolution
/// behavior driven by the ctor's <c>defaultResponseFormat</c> parameter and
/// <c>ContainerComposition.ResolveFormat</c> (empty container Format inherits the transport default;
/// an explicit Format resolves via <see cref="FormatTypes.ByName"/>; an explicit but unregistered
/// Format resolves to <see cref="FormatTypes.NotFound"/> — no silent substitute).
/// </summary>
public sealed class GenericDataStoreBuilderTests
{
    private static DataStoreImplementationConfiguration CreateStoreConfig(string? containerFormat, string? containerTypeId = "Endpoint")
    {
        var storeId = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var containerId = Guid.NewGuid();

        return new DataStoreImplementationConfiguration
        {
            Id = storeId,
            Name = "TestStore",
            Paths =
            [
                new DataPathConfiguration
                {
                    Id = pathId,
                    Name = "TestPath",
                    Containers =
                    [
                        new DataContainerConfiguration
                        {
                            Id = containerId,
                            Name = "TestContainer",
                            Format = containerFormat,
                            TypeId = containerTypeId,
                            Fields = [],
                        },
                    ],
                },
            ],
        };
    }

    private static async Task<IDataContainer> BuildSingleContainer(IFormatType defaultResponseFormat, string? containerFormat)
    {
        var builder = new GenericDataStoreBuilder(defaultResponseFormat);
        var configureResult = builder.Configure(CreateStoreConfig(containerFormat));
        configureResult.IsSuccess.ShouldBeTrue();

        var buildResult = await builder.Build(TestContext.Current.CancellationToken);
        buildResult.IsSuccess.ShouldBeTrue();
        buildResult.Value.ShouldNotBeNull();

        return buildResult.Value.Paths.Single().Containers.Single();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildContainerFormatResolvesToDefaultWhenContainerFormatIsEmpty()
    {
        // Arrange
        var defaultFormat = FormatTypes.ByName("Json");

        // Act
        var container = await BuildSingleContainer(defaultFormat, containerFormat: null);

        // Assert
        container.Format.ShouldBeSameAs(defaultFormat);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildContainerFormatResolvesViaFormatTypesByNameWhenContainerFormatIsExplicit()
    {
        // Arrange — the default is deliberately a DIFFERENT format so success proves the explicit
        // value (not the default) drove resolution.
        var defaultFormat = FormatTypes.ByName("Xml");

        // Act
        var container = await BuildSingleContainer(defaultFormat, containerFormat: "Json");

        // Assert
        container.Format.ShouldBeSameAs(FormatTypes.ByName("Json"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildContainerFormatResolvesToNotFoundWhenContainerFormatIsExplicitAndInvalid()
    {
        // Arrange — a registered default is supplied to prove it is NOT used as a substitute.
        var defaultFormat = FormatTypes.ByName("Json");

        // Act
        var container = await BuildSingleContainer(defaultFormat, containerFormat: "NotARealFormat");

        // Assert — no-fallback rule: an explicit-but-unregistered Format fails loud as NotFound,
        // never silently substituting the transport default.
        container.Format.ShouldBeSameAs(FormatTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildContainerFormatResolvesToNotFoundWhenContainerFormatEmptyAndNoDefaultDeclared()
    {
        // Arrange — the caller supplies FormatTypes.NotFound when the transport declares no default
        // (documented ctor contract); a missing default must fail loud, never a silent Tabular fallback.
        var defaultFormat = FormatTypes.NotFound;

        // Act
        var container = await BuildSingleContainer(defaultFormat, containerFormat: null);

        // Assert
        container.Format.ShouldBeSameAs(FormatTypes.NotFound);
    }

    // FDW-6387: a container type name that ContainerTypes does not hold answers the NotFound placeholder, which is never a container's type.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABuildRefusesAContainerWhoseTypeIsNotRegisteredAndNamesBoth()
    {
        var builder = new GenericDataStoreBuilder(FormatTypes.ByName("Json"));
        builder.Configure(CreateStoreConfig(containerFormat: null, containerTypeId: "NoSuchContainerType")).IsSuccess.ShouldBeTrue();

        var buildResult = await builder.Build(TestContext.Current.CancellationToken);

        buildResult.IsSuccess.ShouldBeFalse();
        buildResult.CurrentMessage.ShouldNotBeNull().ShouldContain("TestContainer");
        buildResult.CurrentMessage.ShouldContain("NoSuchContainerType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AContainerThatStatesTheEndpointTypeIsTheEndpointOptionNotThePlaceholder()
    {
        var container = await BuildSingleContainer(FormatTypes.ByName("Json"), containerFormat: null);

        container.ContainerType.ShouldNotBeSameAs(ContainerTypes.NotFound);
        container.ContainerType.Name.ShouldBe("Endpoint");
    }

    // FDW-6387: a container states its type; none is assumed for it, so a missing TypeId is refused and named, not read as Endpoint.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABuildRefusesAContainerThatStatesNoTypeAndNamesIt()
    {
        var builder = new GenericDataStoreBuilder(FormatTypes.ByName("Json"));
        builder.Configure(CreateStoreConfig(containerFormat: null, containerTypeId: null)).IsSuccess.ShouldBeTrue();

        var buildResult = await builder.Build(TestContext.Current.CancellationToken);

        buildResult.IsSuccess.ShouldBeFalse();
        buildResult.CurrentMessage.ShouldNotBeNull().ShouldContain("TestContainer");
        buildResult.CurrentMessage.ShouldContain("states no container type");
    }
}
