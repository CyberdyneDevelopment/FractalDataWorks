using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// The maximum length a container declaration states for a field reaches the built container's schema (FDW-6581).
/// </summary>
/// <remarks>
/// Why this exists: the schema JSON binds <c>MaxLength</c> into the field configuration, but the builder made each field without it and the field's metadata was null, so the
/// limits of the data option rules (FDW-6575) could never be read from a built container. The container here is built by the real builder, not a field made by hand.
/// </remarks>
public sealed class ContainerFieldMaxLengthTests
{
    private static async Task<IDataContainer> Build(params DataContainerFieldConfiguration[] fields)
    {
        var store = new DataStoreImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "ConfigurationDb",
            Paths =
            [
                new DataPathConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = "data",
                    Containers = [new DataContainerConfiguration { Id = Guid.NewGuid(), Name = "DataConfigurationRule", Fields = [.. fields] }],
                },
            ],
        };

        var builder = new GenericDataStoreBuilder(FormatTypes.NotFound);
        builder.Configure(store).IsSuccess.ShouldBeTrue();
        var built = await builder.Build(TestContext.Current.CancellationToken);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        return built.Value!.Paths.Single().Containers.Single();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADeclaredMaxLengthIsReadBackFromTheBuiltContainersSchemaUnderTheMaxLengthKey()
    {
        var container = await Build(new DataContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "Name", Ordinal = 0, MaxLength = 200 });

        container.Schema.Fields.Single().Metadata.ShouldNotBeNull()["MaxLength"].ShouldBe(200);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFieldThatDeclaresNoMaxLengthHasNoMaxLengthEntry()
    {
        var container = await Build(new DataContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "Description", Ordinal = 0 });

        container.Schema.Fields.Single().Metadata.ShouldBeNull();
    }
}
