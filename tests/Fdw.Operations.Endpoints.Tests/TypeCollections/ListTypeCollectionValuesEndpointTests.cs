using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// Unit tests for the unknown-collection path of <see cref="ListTypeCollectionValuesEndpointBase"/>.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class ListTypeCollectionValuesEndpointTests
{
    private sealed class Endpoint : ListTypeCollectionValuesEndpointBase
    {
        public Endpoint() : base(NullLogger<ListTypeCollectionValuesEndpointBase>.Instance)
        {
        }

        public Task<Fdw.Results.IGenericResult<System.Collections.Generic.List<TypeCollectionValueSummaryDto>>> Load(string name) =>
            LoadItems(new GetTypeCollectionValuesRequest { CollectionName = name }, CancellationToken.None);
    }

    [Fact]
    public async Task UnknownCollectionIsAFailure()
    {
        var result = await new Endpoint().Load("NoSuchThing");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task UnknownCollectionMapsToNotFound()
    {
        var result = await new Endpoint().Load("NoSuchThing");

        var (status, _) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(404);
    }
}
