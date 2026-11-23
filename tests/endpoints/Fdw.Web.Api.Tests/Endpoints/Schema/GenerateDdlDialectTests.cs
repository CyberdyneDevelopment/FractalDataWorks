using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Schema.Ddl;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Endpoints.Ddl;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;

namespace Fdw.Web.Api.Tests.Endpoints.Schema;

/// <summary>
/// Tests for <see cref="GenerateDdlEndpointBase"/> (FDW-5959): the DDL dialect comes from the connection, so a connection
/// that cannot say its dialect is refused, and is never given SQL Server text (or SQL Server text with another engine's quotes).
/// </summary>
public sealed class GenerateDdlDialectTests
{
    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("Sqlite")]
    [InlineData("NoSuchEngine")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AConnectionWhoseTypeOffersNoDialectIsRefusedWithAClientStatus(string connectionType)
    {
        var endpoint = Factory.Create<Probe>(new Engine(connectionType));

        await endpoint.HandleAsync(Stated("Main"), TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBeInRange(400, 499);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheScriptIsTheGeneratorsAndTheEndpointWritesNoEngineTextOfItsOwn()
    {
        var generator = new Mock<IDdlGenerator>();
        generator
            .Setup(g => g.GenerateScript(It.IsAny<IReadOnlyList<IDdlCommand>>()))
            .Returns(GenericResult<string>.Success("-- the generator's script"));
        var endpoint = Factory.Create<Probe>(new Engine("Dialected", generator.Object));

        await endpoint.HandleAsync(Stated("Main"), TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        generator.Verify(g => g.GenerateScript(It.IsAny<IReadOnlyList<IDdlCommand>>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AGeneratorThatRefusesTheScriptIsAnsweredFromItsFailure()
    {
        var generator = new Mock<IDdlGenerator>();
        generator
            .Setup(g => g.GenerateScript(It.IsAny<IReadOnlyList<IDdlCommand>>()))
            .Returns(GenericResult<string>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired")));
        var endpoint = Factory.Create<Probe>(new Engine("Dialected", generator.Object));

        await endpoint.HandleAsync(Stated("Main"), TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AChoiceTheRequestLeavesOutIsRefusedAndNothingIsDiscovered()
    {
        var engine = new Engine("Dialected", new Mock<IDdlGenerator>().Object);
        var endpoint = Factory.Create<Probe>(engine);

        await endpoint.HandleAsync(new GenerateDdlRequest { Name = "Main" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
        engine.Discovered.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheChoicesTheRequestStatesAreTheOnesDiscoveryIsGiven()
    {
        var generator = new Mock<IDdlGenerator>();
        generator
            .Setup(g => g.GenerateScript(It.IsAny<IReadOnlyList<IDdlCommand>>()))
            .Returns(GenericResult<string>.Success("-- script"));
        var engine = new Engine("Dialected", generator.Object);
        var endpoint = Factory.Create<Probe>(engine);

        await endpoint.HandleAsync(
            new GenerateDdlRequest
            {
                Name = "Main",
                ExcludedSchemas = ["sys"],
                IncludeOnlySchemas = ["dbo"],
                DiscoverViews = true,
                DiscoverIndexes = false,
                DiscoverForeignKeys = true,
                DiscoverDescriptions = false,
            },
            TestContext.Current.CancellationToken);

        var options = engine.Discovered.ShouldHaveSingleItem();
        options.ExcludedSchemas.ShouldBe(["sys"]);
        options.IncludeOnlySchemas.ShouldBe(["dbo"]);
        options.DiscoverViews.ShouldBeTrue();
        options.DiscoverIndexes.ShouldBeFalse();
        options.DiscoverForeignKeys.ShouldBeTrue();
        options.DiscoverDescriptions.ShouldBeFalse();
    }

    // A request that states every discovery choice: what these tests are not about is not left out.
    private static GenerateDdlRequest Stated(string name) => new()
    {
        Name = name,
        ExcludedSchemas = [],
        DiscoverViews = false,
        DiscoverIndexes = false,
        DiscoverForeignKeys = false,
        DiscoverDescriptions = false,
    };

    // The engine the connection's type is named for, and the generator its type offers when a test gives one; a type of its own
    // so the endpoint's constructor can be given it.
    private sealed record Engine(string ConnectionType, IDdlGenerator? Generator = null)
    {
        // The options discovery was given, one per call.
        public List<DataStoreDiscoveryOptions> Discovered { get; } = [];
    }

    private sealed class Probe(Engine engine) : GenerateDdlEndpointBase
    {
        protected override Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(
            string connectionName,
            DataStoreDiscoveryOptions options,
            CancellationToken ct)
        {
            engine.Discovered.Add(options);
            return Task.FromResult(GenericResult<IReadOnlyList<IStorageContainer>>.Success([]));
        }

        protected override Task<string?> GetConnectionType(string connectionName, CancellationToken ct)
            => Task.FromResult<string?>(engine.ConnectionType);

        protected override IGenericResult<IDdlGenerator> DdlGeneratorFor(string connectionType)
            => engine.Generator is { } generator ? GenericResult<IDdlGenerator>.Success(generator) : base.DdlGeneratorFor(connectionType);
    }
}
