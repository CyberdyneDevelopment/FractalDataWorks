using System;
using Fdw.Configuration;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1194: the data set and data store lookups sit under the resources they are gated by, the data set lookup offers
/// every row its list offers, and the data store lookup never offers a store before the stores the host declared are known.
/// </summary>
public sealed class LookupEndpointBasesTests
{
    [Fact]
    public void TheDataSetLookupIsRoutedUnderTheDataSetsResourceAndOffersEveryRow()
    {
        var endpoint = new DataSetLookup();

        endpoint.RegisteredRoute.ShouldBe("/datasets/lookup");
        endpoint.RegisteredPolicy.ShouldBe("datasets:read");
        endpoint.Offers(new DomainConfiguration { Name = "Breweries" }).ShouldBeTrue();
    }

    [Fact]
    public void TheDataStoreLookupIsRoutedUnderTheDataStoresResource()
    {
        var endpoint = new DataStoreLookup();

        endpoint.RegisteredRoute.ShouldBe("/datastores/lookup");
        endpoint.RegisteredPolicy.ShouldBe("datastores:read");
    }

    // The host-declared stores are read with the page; deciding a row before then would offer one the list never offers.
    [Fact]
    public void TheDataStoreLookupRefusesToDecideARowBeforeItsPageIsLoaded()
    {
        Should.Throw<InvalidOperationException>(() => new DataStoreLookup().Offers(new DataStoreDomainConfiguration { Name = "PublicDataSink", Implementation = "MsSql" }));
    }

    [Fact]
    public void ThePathContainersEndpointNamesThePathInTheRoute()
    {
        var all = new ContainerSearch();
        var onePath = new PathContainers();

        all.RegisteredRoute.ShouldBe("/datastores/{Name}/containers");
        all.NamesPathInRoute.ShouldBeFalse();
        onePath.RegisteredRoute.ShouldBe("/datastores/{Name}/paths/{PathId}/containers");
        onePath.NamesPathInRoute.ShouldBeTrue();
        all.RegisteredPolicy.ShouldBe("datastores:read");
        onePath.RegisteredPolicy.ShouldBe("datastores:read");
    }

    private sealed class DataSetLookup : LookupDataSetsEndpointBase
    {
        public DataSetLookup()
            : base(NullLogger<LookupDataSetsEndpointBase>.Instance, null!)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public bool Offers(DomainConfiguration row) => Includes(row);
    }

    private sealed class DataStoreLookup : LookupDataStoresEndpointBase
    {
        public DataStoreLookup()
            : base(NullLogger<LookupDataStoresEndpointBase>.Instance, null!, null!)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public bool Offers(DataStoreDomainConfiguration row) => Includes(row);
    }

    private sealed class ContainerSearch : SearchDataStoreContainersEndpointBase
    {
        public ContainerSearch()
            : base(NullLogger<SearchDataStoreContainersEndpointBase>.Instance, null!)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public bool NamesPathInRoute => PathInRoute;
    }

    private sealed class PathContainers : ListDataStorePathContainersEndpointBase
    {
        public PathContainers()
            : base(NullLogger<ListDataStorePathContainersEndpointBase>.Instance, null!)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;

        public bool NamesPathInRoute => PathInRoute;
    }
}
