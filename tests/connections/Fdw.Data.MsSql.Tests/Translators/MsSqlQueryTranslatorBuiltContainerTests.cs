using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// A page of a container the way the running code builds it (FDW-3454).
/// </summary>
/// <remarks>
/// Why these exist: the paging tests in <see cref="MsSqlQueryTranslatorTests"/> hand the translator a mocked container whose key
/// was set by hand, so they could not see that the builder dropped the key's fields. Here the container is built by the real
/// <see cref="DataStoreBuilderBase"/> from the configuration a stored container composes to, as a real
/// <see cref="MsSqlTableContainer"/>, and the real translator pages it. The builder below overrides only what the SQL Server
/// store builder overrides: how a field and a container are made. Key resolution is the base's.
/// </remarks>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlQueryTranslatorBuiltContainerTests
{
    private static readonly Guid CodeFieldId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private sealed class TableStoreBuilder : DataStoreBuilderBase
    {
        protected override IDataField BuildField(DataContainerFieldConfiguration fieldCfg)
            => new MsSqlDataField(
                fieldCfg.Name, fieldCfg.Description, fieldCfg.Ordinal, fieldCfg.IsNullable,
                (DataTypeOptionBase)MsSqlNativeTypeResolver.Resolve(fieldCfg.DataType),
                precision: null, scale: null, maxLength: null, collation: null, DatumTypes.ById(fieldCfg.DatumTypeId));

        protected override IDataContainer BuildContainer(
            DataContainerConfiguration containerCfg,
            IDataNodePath parent,
            IReadOnlyList<IDataField> fields,
            IReadOnlyList<IContainerKey> keys,
            IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys)
            => new MsSqlTableContainer(
                containerCfg.Name, containerCfg.Description, parent, fields.OfType<IMsSqlDataField>().ToList(), keys, referencingKeys,
                new DatabasePath(null, parent.Name, containerCfg.Name), FormatTypes.Tabular, new Dictionary<string, object>(StringComparer.Ordinal));
    }

    // The seeded NflTeams container's shape: a table with a Surrogate key on Code, cited by the field's id as a stored key does.
    private static async Task<IDataContainer> BuildTeam(params DataContainerKeyConfiguration[] keys)
    {
        var attribute = DatumTypes.ByName("Attribute").Id;
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
                            TypeId = "Table",
                            Fields =
                            [
                                new DataContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "Name", DataType = "nvarchar", Ordinal = 0, DatumTypeId = attribute },
                                new DataContainerFieldConfiguration { Id = CodeFieldId, Name = "Code", DataType = "nvarchar", Ordinal = 1, DatumTypeId = attribute },
                            ],
                            Keys = [.. keys],
                        },
                    ],
                },
            ],
        };

        var builder = new TableStoreBuilder();
        builder.Configure(store).IsSuccess.ShouldBeTrue();
        var built = await builder.Build(TestContext.Current.CancellationToken);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        return built.Value!.Paths.Single().Containers.Single();
    }

    private static DataContainerKeyConfiguration SurrogateOnCode() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Surrogate_Team",
        TypeId = "Surrogate",
        KeyFields = [new DataContainerKeyFieldConfiguration { Id = Guid.NewGuid(), DataContainerFieldId = CodeFieldId, Ordinal = 0 }],
    };

    private static QueryCommand<Dictionary<string, object?>> APage()
        => new() { Paging = new PagingExpression { Skip = 0, Take = 50 } };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APageOfABuiltContainerIsOrderedByItsSurrogateKey()
    {
        var container = await BuildTeam(SurrogateOnCode());

        var result = await new MsSqlQueryTranslator().Translate(APage(), container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldContain("ORDER BY [Code] OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY");
        result.Value.CommandText.ShouldNotContain("ORDER BY [Name]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APageOfABuiltContainerThatDeclaresNoKeyIsRefusedNamingIt()
    {
        var container = await BuildTeam();

        var result = await new MsSqlQueryTranslator().Translate(APage(), container, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldEndWith("-21020");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Team");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APageWithAStatedOrderingOfABuiltContainerThatDeclaresNoKeyNeedsNoKey()
    {
        var container = await BuildTeam();
        var page = new QueryCommand<Dictionary<string, object?>>
        {
            Paging = new PagingExpression { Skip = 0, Take = 50 },
            Ordering = new OrderingExpression { OrderedFields = [new OrderedField { PropertyName = "Name", Direction = SortDirections.Ascending }] },
        };

        var result = await new MsSqlQueryTranslator().Translate(page, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldContain("ORDER BY [Name] ASC");
    }
}
