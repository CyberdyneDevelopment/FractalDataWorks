using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5455: the API states each component kind a Graph data set refuses, with its family and the code a save answers; with the allowed list it covers the catalogue once.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5455")]
public sealed class GraphRefusedComponentTypesTests
{
    private sealed class RefusedEndpoint : ListGraphRefusedComponentTypesEndpointBase
    {
        public RefusedEndpoint() : base(NullLogger<ListGraphRefusedComponentTypesEndpointBase>.Instance)
        {
        }

        public Task<IGenericResult<List<GraphRefusedComponentTypePayload>>> Load(CancellationToken ct) => LoadItems(ct);
    }

    private sealed class AllowedEndpoint : ListGraphComponentTypesEndpointBase
    {
        public AllowedEndpoint() : base(NullLogger<ListGraphComponentTypesEndpointBase>.Instance)
        {
        }

        public Task<IGenericResult<List<GraphComponentTypePayload>>> Load(CancellationToken ct) => LoadItems(ct);
    }

    [Theory]
    [InlineData("ContainerDestination", "Destination", "GraphDataSetDestinationRefused")]
    [InlineData("ExecuteSql", "Action", "GraphDataSetActionRefused")]
    [InlineData("ForEach", "Loop", "GraphDataSetLoopRefused")]
    public async Task AKindIsListedWithItsFamilyAndTheCodeASaveAnswers(string kind, string family, string codeName)
    {
        var answer = await new RefusedEndpoint().Load(TestContext.Current.CancellationToken);

        var listed = answer.Value.ShouldNotBeNull().Single(refused => string.Equals(refused.Name, kind, StringComparison.Ordinal));
        listed.Family.ShouldBe(family);
        listed.Code.ShouldBe(DataServiceResultCodes.ByName(codeName).Code);
        listed.Code.ShouldStartWith("DATA-");
    }

    [Fact]
    public async Task AllowedAndRefusedKindsCoverTheCatalogueExactlyOnce()
    {
        var refused = (await new RefusedEndpoint().Load(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().Select(kind => kind.Name).ToList();
        var allowed = (await new AllowedEndpoint().Load(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().Select(kind => kind.Name).ToList();

        allowed.Intersect(refused, StringComparer.OrdinalIgnoreCase).ShouldBeEmpty();
        allowed.Concat(refused).Order(StringComparer.OrdinalIgnoreCase).ShouldBe([.. ComponentTypes.All().Select(type => type.Name).Order(StringComparer.OrdinalIgnoreCase)]);
    }
}
