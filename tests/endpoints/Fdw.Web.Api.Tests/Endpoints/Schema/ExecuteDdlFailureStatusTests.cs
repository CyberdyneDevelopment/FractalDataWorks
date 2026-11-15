using FastEndpoints;
using Fdw.Results;
using Fdw.Schema.Endpoints.Ddl;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Data.Results;

namespace Fdw.Web.Api.Tests.Endpoints.Schema;

/// <summary>
/// Tests for <see cref="ExecuteDdlEndpointBase"/> (FDW-5958): a failed DDL execution answers the status its failure's code
/// means, as every other failure of the endpoint does, and not a fixed 500 whatever went wrong.
/// </summary>
public sealed class ExecuteDdlFailureStatusTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusalForABadRequestAnswers400()
    {
        var endpoint = Factory.Create<Probe>(new Outcome(GenericResult.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"))));

        await endpoint.HandleAsync(Request(), TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AFailureForSomethingThatIsNotThereAnswers404()
    {
        var endpoint = Factory.Create<Probe>(new Outcome(GenericResult.Failure(
            DataServiceResultCodes.ByName("DataSetNotFound"),
            ResultDetails.Create().With("DataSetName", "Main"))));

        await endpoint.HandleAsync(Request(), TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnUnexpectedFailureStillAnswers500()
    {
        var endpoint = Factory.Create<Probe>(new Outcome(GenericResult.Failure(DataServiceResultCodes.ByName("ObjectConversionFailed"))));

        await endpoint.HandleAsync(Request(), TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ASuccessAnswers200()
    {
        var endpoint = Factory.Create<Probe>(new Outcome(GenericResult.Success()));

        await endpoint.HandleAsync(Request(), TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
    }

    private static ExecuteDdlRequest Request() => new()
    {
        Name = "Main",
        TableName = "Teams",
        Columns = [new DdlColumnRequest { Name = "Id", SqlType = "INT", IsRequired = true }],
    };

    // The result the hook answers with; a type of its own so the endpoint's constructor can be given it.
    private sealed record Outcome(IGenericResult Result);

    private sealed class Probe(Outcome outcome) : ExecuteDdlEndpointBase
    {
        protected override Task<IGenericResult> ExecuteDdl(ExecuteDdlRequest request, CancellationToken ct)
            => Task.FromResult(outcome.Result);
    }
}
