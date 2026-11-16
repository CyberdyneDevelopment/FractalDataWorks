using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.ColumnValues;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5666: the values route sits under the container it reads, is gated by reading the data store, hands the whole request
/// to the search and answers its page or its failure as it is.
/// </summary>
public sealed class ListContainerFieldValuesEndpointTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheRouteNamesTheContainerAndColumnAndIsGatedByReadingTheStore()
    {
        var endpoint = new Values(new RecordingValues(Page()));

        endpoint.RegisteredRoute.ShouldBe("/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/fields/{FieldName}/values");
        endpoint.RegisteredPolicy.ShouldBe("datastores:read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheWholeRequestReachesTheSearch()
    {
        var search = new RecordingValues(Page());

        await new Values(search).Find(new ContainerFieldValuesRequest
        {
            Name = "Places", PathName = "dbo", ContainerName = "Visits", FieldName = "City",
            Q = "ber", Skip = 12, Take = 12, Chosen = ["Bern"], LeaveOutChosen = true,
        }, TestContext.Current.CancellationToken);

        var asked = search.Asked.ShouldNotBeNull();
        (asked.DataStore, asked.Path, asked.Container, asked.Column).ShouldBe(("Places", "dbo", "Visits", "City"));
        (asked.Search, asked.Skip, asked.Take, asked.LeaveOutChosen).ShouldBe(("ber", 12, (int?)12, true));
        asked.Chosen.ShouldBe(["Bern"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ThePageIsAnsweredWithEachValuesRowsAndChoice()
    {
        var found = await new Values(new RecordingValues(Page())).Find(Request(), TestContext.Current.CancellationToken);

        var page = found.Value.ShouldNotBeNull();
        page.HasMore.ShouldBeTrue();
        page.Items.ShouldHaveSingleItem();
        (page.Items[0].Value, page.Items[0].Rows, page.Items[0].Chosen).ShouldBe(("Berlin", 5L, true));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusalIsAnsweredWithItsOwnCode()
    {
        var refused = GenericResult<ColumnValuesPage>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        var found = await new Values(new RecordingValues(refused)).Find(Request(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Code.ShouldBeSameAs(DataServiceResultCodes.ByName("DataStoreNameRequired"));
    }

    private static ContainerFieldValuesRequest Request()
        => new() { Name = "Places", PathName = "dbo", ContainerName = "Visits", FieldName = "City" };

    private static IGenericResult<ColumnValuesPage> Page()
        => GenericResult<ColumnValuesPage>.Success(new ColumnValuesPage
        {
            Items = [new ColumnValue { Value = "Berlin", Rows = 5, Chosen = true }],
            HasMore = true,
        });

    private sealed class RecordingValues : IContainerColumnValues
    {
        private readonly IGenericResult<ColumnValuesPage> _answer;

        public RecordingValues(IGenericResult<ColumnValuesPage> answer)
        {
            _answer = answer;
        }

        public ColumnValuesQuery? Asked { get; private set; }

        public Task<IGenericResult<ColumnValuesPage>> Search(ColumnValuesQuery query, CancellationToken cancellationToken = default)
        {
            Asked = query;
            return Task.FromResult(_answer);
        }
    }

    private sealed class Values : ListContainerFieldValuesEndpointBase
    {
        public Values(IContainerColumnValues values)
            : base(NullLogger<ListContainerFieldValuesEndpointBase>.Instance, values)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public Task<IGenericResult<ContainerFieldValuesPayload?>> Find(ContainerFieldValuesRequest request, CancellationToken cancellationToken)
            => FindByIdentifier(request, cancellationToken);
    }
}
