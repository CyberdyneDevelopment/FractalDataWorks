using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Schema.Ddl;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Endpoints.Ddl;
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

        await endpoint.HandleAsync(new GenerateDdlRequest { Name = "Main" }, TestContext.Current.CancellationToken);

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

        await endpoint.HandleAsync(new GenerateDdlRequest { Name = "Main" }, TestContext.Current.CancellationToken);

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

        await endpoint.HandleAsync(new GenerateDdlRequest { Name = "Main" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
    }

    // The engine the connection's type is named for, and the generator its type offers when a test gives one; a type of its own
    // so the endpoint's constructor can be given it.
    private sealed record Engine(string ConnectionType, IDdlGenerator? Generator = null);

    private sealed class Probe(Engine engine) : GenerateDdlEndpointBase
    {
        protected override Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(string connectionName, CancellationToken ct)
            => Task.FromResult(GenericResult<IReadOnlyList<IStorageContainer>>.Success([]));

        protected override Task<string?> GetConnectionType(string connectionName, CancellationToken ct)
            => Task.FromResult<string?>(engine.ConnectionType);

        protected override IGenericResult<IDdlGenerator> DdlGeneratorFor(string connectionType)
            => engine.Generator is { } generator ? GenericResult<IDdlGenerator>.Success(generator) : base.DdlGeneratorFor(connectionType);
    }
}
