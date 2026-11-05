using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// The key fields a built container carries (FDW-3454).
/// </summary>
/// <remarks>
/// Why these exist: a key stored in ConfigurationDb names its field by <c>DataContainerFieldId</c> and carries no name, while the
/// schema JSON a host ships names it and carries no id. The builder read only the name, so every stored key was built with no
/// fields, and nothing noticed until a paged read needed the container's primary key. The configuration here is the shape the
/// stored rows compose to, built by the real builder, not a container with its key set by hand.
/// </remarks>
public sealed class ContainerKeyFieldResolutionTests
{
    private static readonly Guid CodeFieldId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static async Task<IDataContainer> Build(DataContainerKeyFieldConfiguration keyField)
    {
        var store = new DataStoreImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "NflDb",
            Paths =
            [
                new DataPathConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = "dbo",
                    Containers =
                    [
                        new DataContainerConfiguration
                        {
                            Id = Guid.NewGuid(),
                            Name = "Team",
                            Fields =
                            [
                                new DataContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "Name", Ordinal = 0 },
                                new DataContainerFieldConfiguration { Id = CodeFieldId, Name = "Code", Ordinal = 1 },
                            ],
                            Keys =
                            [
                                new DataContainerKeyConfiguration { Id = Guid.NewGuid(), Name = "Surrogate_Team", TypeId = "Surrogate", KeyFields = [keyField] },
                            ],
                        },
                    ],
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
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoredKeyFieldThatCitesItsFieldByIdIsBuiltWithThatField()
    {
        var container = await Build(new DataContainerKeyFieldConfiguration { Id = Guid.NewGuid(), DataContainerFieldId = CodeFieldId, Ordinal = 0 });

        container.Keys.Single().KeyFields.Single().LocalField.Name.ShouldBe("Code");
        container.GetPrimaryKeyFieldName().ShouldBe("Code");
        container.GetDeclaredOrderFieldName().ShouldBe("Code");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AShippedKeyFieldThatNamesItsFieldIsBuiltWithThatField()
    {
        var container = await Build(new DataContainerKeyFieldConfiguration { Name = "Code", Ordinal = 0 });

        container.Keys.Single().KeyFields.Single().LocalField.Name.ShouldBe("Code");
        container.GetDeclaredOrderFieldName().ShouldBe("Code");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyFieldThatCitesAnIdNoFieldHasIsNotReadAsItsNameAndDeclaresNoOrder()
    {
        // The id matches no field of the container; its Name matches one. Reading the name in place of the id it stated would
        // be a substitution, so the key is built with no fields and the container declares no order.
        var container = await Build(new DataContainerKeyFieldConfiguration { Id = Guid.NewGuid(), DataContainerFieldId = Guid.NewGuid(), Name = "Code", Ordinal = 0 });

        container.Keys.Single().KeyFields.ShouldBeEmpty();
        container.GetDeclaredOrderFieldName().ShouldBeNull();
    }
}
